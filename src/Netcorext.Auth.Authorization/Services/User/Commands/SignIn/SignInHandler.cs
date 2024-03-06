using FreeRedis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netcorext.Algorithms;
using Netcorext.Auth.Authorization.Models;
using Netcorext.Auth.Authorization.Settings;
using Netcorext.Auth.Enums;
using Netcorext.Auth.Extensions;
using Netcorext.Auth.Utilities;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.EntityFramework.UserIdentityPattern.Extensions;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Hash;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authorization.Services.User.Commands;

public class SignInHandler : IRequestHandler<SignIn, Result<TokenResult>>
{
    private readonly DatabaseContext _context;
    private readonly RedisClient _redis;
    private readonly ISnowflake _snowflake;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JwtGenerator _jwtGenerator;
    private readonly ConfigSettings _config;
    private readonly AuthOptions _authOptions;

    public SignInHandler(DatabaseContextAdapter context, RedisClient redis, ISnowflake snowflake, IHttpContextAccessor httpContextAccessor, JwtGenerator jwtGenerator, IOptions<ConfigSettings> config, IOptions<AuthOptions> autoOptions)
    {
        _context = context;
        _redis = redis;
        _snowflake = snowflake;
        _httpContextAccessor = httpContextAccessor;
        _jwtGenerator = jwtGenerator;
        _config = config.Value;
        _authOptions = autoOptions.Value;
    }

    public async Task<Result<TokenResult>> Handle(SignIn request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.User>();

        if (!await ds.AnyAsync(t => t.NormalizedUsername == request.Username.ToUpper(), cancellationToken))
            return Result<TokenResult>.UsernameOrPasswordIncorrect;

        var entity = await ds.Include(t => t.Roles)
                             .ThenInclude(t => t.Role)
                             .FirstAsync(t => t.NormalizedUsername == request.Username.ToUpper(), cancellationToken);

        _context.Entry(entity).UpdateProperty(t => t.LastSignInDate, DateTimeOffset.UtcNow);
        _context.Entry(entity).UpdateProperty(t => t.LastSignInIp, _httpContextAccessor.HttpContext?.GetIp());

        if (entity.Disabled)
        {
            await SetSignInFailureStateAsync(entity, cancellationToken);

            return Result<TokenResult>.AccountIsDisabled;
        }

        var passwordHash = request.Password.Pbkdf2HashCode(entity.CreationDate.ToUnixTimeMilliseconds());

        if (entity.Password != passwordHash)
        {
            await SetSignInFailureStateAsync(entity, cancellationToken);

            return Result<TokenResult>.UsernameOrPasswordIncorrect;
        }

        if (entity.TwoFactorEnabled)
        {
            if (request.Otp.IsEmpty() && !entity.OtpBound)
            {
                _context.Entry(entity).UpdateProperty(t => t.Otp, Otp.GenerateRandomKey().ToBase32String());

                await _context.SaveChangesAsync(cancellationToken);

                return new Result<TokenResult>
                       {
                           Code = Result.RequiredTwoFactorAuthenticationBinding,
                           Message = string.Format(_authOptions.OtpAuthScheme, _authOptions.Issuer, entity.Username, entity.Otp)
                       };
            }

            if (request.Otp.IsEmpty() || !Otp.ValidateCode(entity.Otp ?? "", request.Otp))
            {
                await SetSignInFailureStateAsync(entity, cancellationToken);

                return Result<TokenResult>.TwoFactorAuthenticationFailed;
            }

            if (!entity.OtpBound)
            {
                _context.Entry(entity).UpdateProperty(t => t.OtpBound, true);
            }
        }

        var signature = $"user:{entity.Id}";
        if (_config.Caches.TryGetValue(ConfigSettings.CACHE_TOKEN_RETAIN, out var cache) && !cache.Key.IsEmpty() && cache.ServerDuration is > 0 && !signature.IsEmpty())
        {
            var cacheResult = await _redis.GetAsync<TokenResult>(cache.Key + ":" + signature);

            if (cacheResult != null)
                return Result<TokenResult>.Success.Clone(cacheResult);
        }

        _context.Entry(entity).UpdateProperty(t => t.AccessFailedCount, 0);

        await _context.SaveChangesAsync(cancellationToken);

        var scope = entity.Roles.Any(t => t.ExpireDate > DateTimeOffset.UtcNow && !t.Role.Disabled)
                        ? entity.Roles
                                .Where(t => t.ExpireDate > DateTimeOffset.UtcNow && !t.Role.Disabled)
                                .Select(t => t.RoleId.ToString()).Aggregate((c, n) => c + " " + n)
                        : null;

        var accessToken = _jwtGenerator.Generate(TokenType.AccessToken, ResourceType.User, entity.Id.ToString(), null, entity.DisplayName, entity.TokenExpireSeconds, scope);

        var refreshToken = entity.AllowedRefreshToken
                               ? _jwtGenerator.Generate(TokenType.RefreshToken, ResourceType.User, entity.Id.ToString(), null, entity.DisplayName, entity.RefreshTokenExpireSeconds, scope)
                               : JwtGenerator.DefaultGenerateEmpty;

        var result = Result<TokenResult>.Success.Clone(new TokenResult
                                                       {
                                                           TokenType = Constants.OAuth.TOKEN_TYPE_BEARER,
                                                           AccessToken = accessToken.Token,
                                                           Scope = scope,
                                                           RefreshToken = refreshToken.Token,
                                                           ExpiresIn = entity.TokenExpireSeconds
                                                       });

        if (cache != null && !cache.Key.IsEmpty() && cache.ServerDuration is > 0)
        {
            await _redis.SetAsync(cache.Key + ":" + signature, result.Content!, cache.ServerDuration.Value);
        }

        var dsToken = _context.Set<Domain.Entities.Token>();

        dsToken.Add(new Domain.Entities.Token
                    {
                        Id = _snowflake.Generate(),
                        ResourceType = ResourceType.User,
                        ResourceId = entity.Id.ToString(),
                        TokenType = result.Content?.TokenType!,
                        AccessToken = accessToken.Token,
                        ExpiresIn = accessToken.ExpiresIn,
                        ExpiresAt = accessToken.ExpiresAt,
                        Scope = result.Content?.Scope,
                        RefreshToken = refreshToken.Token,
                        RefreshExpiresIn = refreshToken.Token.IsEmpty() ? null : refreshToken.ExpiresIn,
                        RefreshExpiresAt = refreshToken.Token.IsEmpty() ? null : refreshToken.ExpiresAt,
                        Revoked = TokenRevoke.None
                    });

        await _context.SaveChangesAsync(cancellationToken);

        await _redis.PublishAsync(_config.Queues[ConfigSettings.QUEUES_USER_SIGN_IN_EVENT], "[{\"Id\":" + entity.Id + ",\"LastSignInDate\":\"" + entity.LastSignInDate?.ToString("O") + "\"}]");

        return result;
    }

    private async Task SetSignInFailureStateAsync(Domain.Entities.User entity, CancellationToken cancellationToken = default)
    {
        entity.AccessFailedCount += 1;

        _context.Entry(entity).Property(t => t.AccessFailedCount).IsModified = true;

        if (!entity.Disabled && _authOptions.LockoutAccessFailedCount.HasValue && entity.AccessFailedCount >= _authOptions.LockoutAccessFailedCount)
        {
            entity.Disabled = true;

            _context.Entry(entity).Property(t => t.Disabled).IsModified = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

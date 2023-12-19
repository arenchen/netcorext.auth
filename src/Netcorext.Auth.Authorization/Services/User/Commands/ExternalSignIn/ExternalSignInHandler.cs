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

public class ExternalSignInHandler : IRequestHandler<ExternalSignIn, Result<TokenResult>>
{
    private readonly DatabaseContext _context;
    private readonly RedisClient _redis;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISnowflake _snowflake;
    private readonly JwtGenerator _jwtGenerator;
    private readonly ConfigSettings _config;
    private readonly AuthOptions _authOptions;

    public ExternalSignInHandler(DatabaseContextAdapter context, RedisClient redis, IHttpContextAccessor httpContextAccessor, ISnowflake snowflake, JwtGenerator jwtGenerator, IOptions<ConfigSettings> config, IOptions<AuthOptions> authOptions)
    {
        _context = context;
        _redis = redis;
        _httpContextAccessor = httpContextAccessor;
        _snowflake = snowflake;
        _jwtGenerator = jwtGenerator;
        _config = config.Value;
        _authOptions = authOptions.Value;
    }

    public async Task<Result<TokenResult>> Handle(ExternalSignIn request, CancellationToken cancellationToken = default)
    {
        var dsExternalLogin = _context.Set<Domain.Entities.UserExternalLogin>();
        var dsUser = _context.Set<Domain.Entities.User>();
        var username = request.Username;
        var creationDate = DateTimeOffset.UtcNow;

        var entity = await dsUser.Include(t => t.Roles)
                                 .ThenInclude(t => t.Role)
                                 .FirstOrDefaultAsync(t => t.NormalizedUsername == username.ToUpper(), cancellationToken);

        var isNewRegister = entity == null;

        if (isNewRegister && request.ThrowErrorWhenUserNotFound)
            return Result<TokenResult>.UsernameOrPasswordIncorrect;

        if (entity != null)
        {
            if (entity.Disabled)
            {
                await SetSignInFailureStateAsync(entity, cancellationToken);

                return Result<TokenResult>.AccountIsDisabled;
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
        }

        var id = entity?.Id ?? (request.CustomId ?? _snowflake.Generate());

        var signature = $"user:{id}";
        if (_config.Caches.TryGetValue(ConfigSettings.CACHE_TOKEN_RETAIN, out var cache) && !cache.Key.IsEmpty() && cache.ServerDuration is > 0 && !signature.IsEmpty())
        {
            var cacheResult = await _redis.GetAsync<TokenResult>(cache.Key + ":" + signature);

            if (cacheResult != null)
                return Result<TokenResult>.Success.Clone(cacheResult);
        }

        entity ??= new Domain.Entities.User
                   {
                       Id = id,
                       Username = username,
                       NormalizedUsername = username.ToUpper(),
                       DisplayName = request.DisplayName ?? request.Username,
                       NormalizedDisplayName = (request.DisplayName ?? request.Username).ToUpper(),
                       Password = Guid.NewGuid().ToString().Pbkdf2HashCode(creationDate.ToUnixTimeMilliseconds()),
                       Email = request.Email,
                       NormalizedEmail = request.Email?.ToUpper(),
                       PhoneNumber = request.PhoneNumber,
                       AllowedRefreshToken = request.AllowedRefreshToken,
                       TokenExpireSeconds = request.TokenExpireSeconds ?? _authOptions.TokenExpireSeconds,
                       RefreshTokenExpireSeconds = request.RefreshTokenExpireSeconds ?? _authOptions.RefreshTokenExpireSeconds,
                       CodeExpireSeconds = request.CodeExpireSeconds ?? _authOptions.CodeExpireSeconds,
                       Roles = request.Roles?
                                      .Select(t => new Domain.Entities.UserRole
                                                   {
                                                       Id = id,
                                                       RoleId = t.RoleId,
                                                       ExpireDate = t.ExpireDate ?? Core.Constants.MaxDateTime
                                                   })
                                      .ToArray() ?? Array.Empty<Domain.Entities.UserRole>()
                   };

        entity.AccessFailedCount = 0;
        entity.LastSignInDate = creationDate;
        entity.LastSignInIp = _httpContextAccessor.HttpContext?.GetIp();

        if (!await dsExternalLogin.AnyAsync(t => t.Provider == request.Provider && t.UniqueId == request.UniqueId, cancellationToken))
        {
            entity.ExternalLogins.Add(new Domain.Entities.UserExternalLogin
                                      {
                                          Id = entity.Id,
                                          Provider = request.Provider,
                                          UniqueId = request.UniqueId
                                      });
        }

        if (isNewRegister)
        {
            dsUser.Add(entity);

            await _context.SaveChangesAsync(e =>
                                            {
                                                e.CreationDate = creationDate;
                                                e.ModificationDate = creationDate;
                                            }, cancellationToken);

            await _context.Entry(entity)
                          .Collection(t => t.Roles)
                          .Query()
                          .Include(t => t.Role)
                          .LoadAsync(cancellationToken);
        }
        else
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        var scope = entity.Roles.Any(t => t.ExpireDate > DateTimeOffset.UtcNow && !t.Role.Disabled)
                        ? entity.Roles
                                .Where(t => t.ExpireDate > DateTimeOffset.UtcNow && !t.Role.Disabled)
                                .Select(t => t.RoleId.ToString()).Aggregate((c, n) => c + " " + n)
                        : null;

        var accessToken = _jwtGenerator.Generate(TokenType.AccessToken, ResourceType.User, entity.Id.ToString(), request.UniqueId, entity.TokenExpireSeconds, scope);

        var refreshToken = entity.AllowedRefreshToken
                               ? _jwtGenerator.Generate(TokenType.RefreshToken, ResourceType.User, entity.Id.ToString(), request.UniqueId, entity.RefreshTokenExpireSeconds, scope, scope)
                               : JwtGenerator.DefaultGenerateEmpty;

        var result = Result<TokenResult>.Success.Clone(new TokenResult
                                                       {
                                                           TokenType = Constants.OAuth.TOKEN_TYPE_BEARER,
                                                           AccessToken = accessToken.Token,
                                                           Scope = scope,
                                                           RefreshToken = refreshToken.Token,
                                                           ExpiresIn = accessToken.ExpiresIn,
                                                           NameId = entity.Id.ToString()
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
        _context.Entry(entity).UpdateProperty(t => t.AccessFailedCount, entity.AccessFailedCount++);

        if (!entity.Disabled && _authOptions.LockoutAccessFailedCount.HasValue && entity.AccessFailedCount >= _authOptions.LockoutAccessFailedCount)
        {
            _context.Entry(entity).UpdateProperty(t => t.Disabled, true);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

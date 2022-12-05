using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netcorext.Algorithms;
using Netcorext.Auth.Authorization.Models;
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
    private readonly HttpContext? _httpContext;
    private readonly ISnowflake _snowflake;
    private readonly JwtGenerator _jwtGenerator;
    private readonly AuthOptions _config;

    public ExternalSignInHandler(DatabaseContext context, IHttpContextAccessor httpContextAccessor, ISnowflake snowflake, JwtGenerator jwtGenerator, IOptions<AuthOptions> config)
    {
        _context = context;
        _httpContext = httpContextAccessor.HttpContext;
        _snowflake = snowflake;
        _jwtGenerator = jwtGenerator;
        _config = config.Value;
    }

    public async Task<Result<TokenResult>> Handle(ExternalSignIn request, CancellationToken cancellationToken = default)
    {
        var dsExternalLogin = _context.Set<Domain.Entities.UserExternalLogin>();
        var dsUser = _context.Set<Domain.Entities.User>();
        var username = request.Username;
        var creationDate = DateTimeOffset.UtcNow;

        var entity = await dsUser.Include(t => t.Roles)
                                 .FirstOrDefaultAsync(t => t.NormalizedUsername == username.ToUpper(), cancellationToken);

        var isNewRegister = entity == null;

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
                               Message = string.Format(_config.OtpAuthScheme, _config.Issuer, entity.Username, entity.Otp)
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

        entity ??= new Domain.Entities.User
                   {
                       Id = id,
                       Username = username,
                       NormalizedUsername = username.ToUpper(),
                       Password = Guid.NewGuid().ToString().Pbkdf2HashCode(creationDate.ToUnixTimeMilliseconds()),
                       Email = request.Email,
                       NormalizedEmail = request.Email?.ToUpper(),
                       PhoneNumber = request.PhoneNumber,
                       AllowedRefreshToken = request.AllowedRefreshToken,
                       TokenExpireSeconds = request.TokenExpireSeconds ?? _config.TokenExpireSeconds,
                       RefreshTokenExpireSeconds = request.RefreshTokenExpireSeconds ?? _config.RefreshTokenExpireSeconds,
                       CodeExpireSeconds = request.CodeExpireSeconds ?? _config.CodeExpireSeconds,
                       Roles = request.Roles?
                                      .Select(t => new Domain.Entities.UserRole
                                                   {
                                                       Id = id,
                                                       RoleId = t.RoleId,
                                                       ExpireDate = t.ExpireDate
                                                   })
                                      .ToArray() ?? Array.Empty<Domain.Entities.UserRole>()
                   };

        entity.AccessFailedCount = 0;
        entity.LastSignInDate = creationDate;
        entity.LastSignInIp = _httpContext?.GetIp();

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
        }
        else
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        var scope = entity.Roles.Any() ? entity.Roles.Select(t => t.RoleId.ToString()).Aggregate((c, n) => c + " " + n) : null;

        var result = Result<TokenResult>.Success.Clone(new TokenResult
                                                       {
                                                           TokenType = Constants.OAuth.TOKEN_TYPE_BEARER,
                                                           AccessToken = _jwtGenerator.Generate(TokenType.AccessToken, ResourceType.User,
                                                                                                entity.Id.ToString(), request.UniqueId, entity.TokenExpireSeconds, scope)
                                                                                      .Token,
                                                           Scope = scope,
                                                           RefreshToken = entity.AllowedRefreshToken
                                                                              ? _jwtGenerator.Generate(TokenType.RefreshToken, ResourceType.User,
                                                                                                       entity.Id.ToString(), request.UniqueId, entity.RefreshTokenExpireSeconds, scope, scope
                                                                                                      )
                                                                                             .Token
                                                                              : null,
                                                           ExpiresIn = entity.TokenExpireSeconds ?? _config.TokenExpireSeconds,
                                                           NameId = entity.Id.ToString()
                                                       });

        var dsToken = _context.Set<Domain.Entities.Token>();

        dsToken.Add(new Domain.Entities.Token
                    {
                        Id = _snowflake.Generate(),
                        ResourceType = ResourceType.User,
                        ResourceId = entity.Id.ToString(),
                        TokenType = result.Content?.TokenType!,
                        AccessToken = result.Content?.AccessToken!,
                        ExpiresIn = result.Content?.ExpiresIn,
                        Scope = result.Content?.Scope,
                        RefreshToken = result.Content?.RefreshToken
                    });

        await _context.SaveChangesAsync(cancellationToken);

        return result;
    }

    private async Task SetSignInFailureStateAsync(Domain.Entities.User entity, CancellationToken cancellationToken = default)
    {
        _context.Entry(entity).UpdateProperty(t => t.AccessFailedCount, entity.AccessFailedCount++);

        if (!entity.Disabled && _config.LockoutAccessFailedCount.HasValue && entity.AccessFailedCount >= _config.LockoutAccessFailedCount)
        {
            _context.Entry(entity).UpdateProperty(t => t.Disabled, true);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
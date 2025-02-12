using System.Security.Claims;
using FreeRedis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netcorext.Algorithms;
using Netcorext.Auth.Authorization.Models;
using Netcorext.Auth.Authorization.Settings;
using Netcorext.Auth.Enums;
using Netcorext.Auth.Extensions;
using Netcorext.Auth.Helpers;
using Netcorext.Auth.Utilities;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.EntityFramework.UserIdentityPattern.Extensions;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Hash;
using Netcorext.Mediator;
using Netcorext.Serialization;

namespace Netcorext.Auth.Authorization.Services.Token.Commands;

public class CreateTokenHandler : IRequestHandler<CreateToken, Result<TokenResult>>
{
    private readonly DatabaseContext _context;
    private readonly ISnowflake _snowflake;
    private readonly JwtGenerator _jwtGenerator;
    private readonly ISerializer _serializer;
    private readonly RedisClient _redis;
    private readonly ConfigSettings _config;
    private readonly AuthOptions _authOptions;

    public CreateTokenHandler(DatabaseContextAdapter context, ISnowflake snowflake, JwtGenerator jwtGenerator, RedisClient redis, ISerializer serializer, IOptions<AuthOptions> authOptions, IOptions<ConfigSettings> config)
    {
        _context = context;
        _snowflake = snowflake;
        _jwtGenerator = jwtGenerator;
        _serializer = serializer;
        _redis = redis;
        _config = config.Value;
        _authOptions = authOptions.Value;
    }

    public async Task<Result<TokenResult>> Handle(CreateToken request, CancellationToken cancellationToken = default)
    {
        if (!await IsValidAsync(request.GrantType))
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                              Error = Constants.OAuth.UNSUPPORTED_GRANT_TYPE,
                                                              ErrorDescription = $"{request.GrantType} GrantType not allowed."
                                                          });

        return request.GrantType switch
               {
                   Constants.OAuth.GRANT_TYPE_CLIENT_CREDENTIALS => await CreateClientCredentialsAsync(request, cancellationToken),
                   Constants.OAuth.GRANT_TYPE_PASSWORD => await CreatePasswordCredentialsAsync(request, cancellationToken),
                   Constants.OAuth.GRANT_TYPE_REFRESH_TOKEN => await CreateRefreshTokenAsync(request, cancellationToken),
                   _ => Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                               {
                                                                   Error = Constants.OAuth.UNSUPPORTED_GRANT_TYPE,
                                                                   ErrorDescription = $"{request.GrantType} GrantType not allowed."
                                                               })
               };
    }

    private async Task<Result<TokenResult>> CreateClientCredentialsAsync(CreateToken request, CancellationToken cancellationToken = default)
    {
        if (request.ClientId.IsEmpty() || !long.TryParse(request.ClientId, out var clientId) || request.ClientSecret.IsEmpty())
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                              Error = Constants.OAuth.INVALID_REQUEST,
                                                              ErrorDescription = Constants.OAuth.INVALID_REQUEST_ID_OR_SECRET_MESSAGE
                                                          });

        var dsClient = _context.Set<Domain.Entities.Client>();

        var client = await dsClient.Include(t => t.Roles)
                                   .ThenInclude(t => t.Role)
                                   .FirstOrDefaultAsync(t => t.Id == clientId, cancellationToken);

        if (client == null)
            return Result<TokenResult>.UsernameOrPasswordIncorrect.Clone(new TokenResult
                                                                         {
                                                                             Error = Constants.OAuth.INVALID_REQUEST,
                                                                             ErrorDescription = Constants.OAuth.INVALID_REQUEST_ID_OR_SECRET_MESSAGE
                                                                         });

        if (client.Disabled)
            return Result<TokenResult>.AccountIsDisabled.Clone(new TokenResult
                                                               {
                                                                   Error = Constants.OAuth.ACCESS_DENIED,
                                                                   ErrorDescription = Constants.OAuth.ACCESS_DENIED_MESSAGE
                                                               });

        var secret = request.ClientSecret!.Pbkdf2HashCode(client.CreationDate.ToUnixTimeMilliseconds());

        if (client.Secret != secret)
            return Result<TokenResult>.UsernameOrPasswordIncorrect.Clone(new TokenResult
                                                                         {
                                                                             Error = Constants.OAuth.INVALID_REQUEST,
                                                                             ErrorDescription = Constants.OAuth.INVALID_REQUEST_ID_OR_SECRET_MESSAGE
                                                                         });

        var signature = $"client:{client.Id}";

        if (_config.Caches.TryGetValue(ConfigSettings.CACHE_TOKEN_RETAIN, out var cache) && !cache.Key.IsEmpty() && cache.ServerDuration is > 0 && !signature.IsEmpty())
        {
            var cacheResult = await _redis.GetAsync<TokenResult>(cache.Key + ":" + signature);

            if (cacheResult != null)
                return Result<TokenResult>.Success.Clone(cacheResult);
        }

        var roles = client.Roles
                          .Where(t => t.ExpireDate > DateTimeOffset.UtcNow && !t.Role.Disabled)
                          .Select(t => new Role
                                       {
                                           Id = t.RoleId,
                                           Name = t.Role.Name,
                                           Priority = t.Role.Priority,
                                           ExpireDate = t.ExpireDate
                                       })
                          .OrderBy(t => t.Priority)
                          .ToArray();

        var scope = roles.Any() ? roles.Select(t => t.Id.ToString()).Aggregate((c, n) => c + " " + n) : null;

        if (!TokenHelper.ScopeCheck(scope, request.Scope))
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                              Error = Constants.OAuth.INVALID_SCOPE,
                                                              ErrorDescription = string.Format(Constants.OAuth.INVALID_SCOPE_MESSAGE, request.Scope)
                                                          });

        var accessToken = _jwtGenerator.Generate(TokenType.AccessToken, ResourceType.Client, client.Id.ToString(), request.UniqueId, client.Name, client.TokenExpireSeconds, request.Scope ?? scope);

        var refreshToken = client.AllowedRefreshToken || _authOptions.AllowedRefreshToken
                               ? _jwtGenerator.Generate(TokenType.RefreshToken, ResourceType.Client, client.Id.ToString(), request.UniqueId, client.Name, client.RefreshTokenExpireSeconds, request.Scope ?? scope)
                               : JwtGenerator.DefaultGenerateEmpty;

        var result = Result<TokenResult>.Success.Clone(new TokenResult
                                                       {
                                                           TokenType = Constants.OAuth.TOKEN_TYPE_BEARER,
                                                           AccessToken = accessToken.Token,
                                                           Scope = request.Scope ?? scope,
                                                           RefreshToken = refreshToken.Token,
                                                           ExpiresIn = accessToken.ExpiresIn,
                                                           NameId = client.Id.ToString(),
                                                           Roles = request.IncludeRolesInfo ? roles : null
                                                       });

        if (cache != null && !cache.Key.IsEmpty() && cache.ServerDuration is > 0)
        {
            if (!await _redis.SetNxAsync(cache.Key + ":" + signature, result.Content!, cache.ServerDuration.Value))
            {
                var cacheResult = await _redis.GetAsync<TokenResult>(cache.Key + ":" + signature);

                if (cacheResult != null)
                    return Result<TokenResult>.Success.Clone(cacheResult);
            }
        }

        var dsToken = _context.Set<Domain.Entities.Token>();

        dsToken.Add(new Domain.Entities.Token
                    {
                        Id = _snowflake.Generate(),
                        ResourceType = ResourceType.Client,
                        ResourceId = client.Id.ToString(),
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

        return result;
    }

    private async Task<Result<TokenResult>> CreatePasswordCredentialsAsync(CreateToken request, CancellationToken cancellationToken = default)
    {
        if (request.ClientId.IsEmpty() || !long.TryParse(request.ClientId, out var clientId) || request.ClientSecret.IsEmpty())
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                              Error = Constants.OAuth.UNAUTHORIZED_CLIENT,
                                                              ErrorDescription = Constants.OAuth.UNAUTHORIZED_CLIENT_MESSAGE
                                                          });

        if (request.Username.IsEmpty() || request.Password.IsEmpty())
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                              Error = Constants.OAuth.INVALID_REQUEST,
                                                              ErrorDescription = Constants.OAuth.INVALID_REQUEST_USERNAME_OR_PASSWORD_MESSAGE
                                                          });

        var dsClient = _context.Set<Domain.Entities.Client>();

        var client = await dsClient.FirstOrDefaultAsync(t => t.Id == clientId, cancellationToken);

        if (client == null)
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                              Error = Constants.OAuth.UNAUTHORIZED_CLIENT,
                                                              ErrorDescription = Constants.OAuth.UNAUTHORIZED_CLIENT_MESSAGE
                                                          });

        if (client.Disabled)
            return Result<TokenResult>.Forbidden.Clone(new TokenResult
                                                       {
                                                           Error = Constants.OAuth.ACCESS_DENIED,
                                                           ErrorDescription = Constants.OAuth.ACCESS_DENIED_MESSAGE
                                                       });

        var secret = request.ClientSecret!.Pbkdf2HashCode(client.CreationDate.ToUnixTimeMilliseconds());

        if (client.Secret != secret)
            return Result<TokenResult>.Forbidden.Clone(new TokenResult
                                                       {
                                                           Error = Constants.OAuth.UNAUTHORIZED_CLIENT,
                                                           ErrorDescription = Constants.OAuth.UNAUTHORIZED_CLIENT_MESSAGE
                                                       });

        var dsUser = _context.Set<Domain.Entities.User>();

        var user = await dsUser.Include(t => t.Roles)
                               .ThenInclude(t => t.Role)
                               .FirstOrDefaultAsync(t => t.NormalizedUsername == request.Username!.ToUpper(), cancellationToken);

        if (user == null)
            return Result<TokenResult>.UsernameOrPasswordIncorrect.Clone(new TokenResult
                                                                         {
                                                                             Error = Constants.OAuth.INVALID_REQUEST,
                                                                             ErrorDescription = Constants.OAuth.INVALID_REQUEST_USERNAME_OR_PASSWORD_MESSAGE
                                                                         });

        if (user.Disabled)
            return Result<TokenResult>.AccountIsDisabled.Clone(new TokenResult
                                                               {
                                                                   Error = Constants.OAuth.ACCESS_DENIED,
                                                                   ErrorDescription = Constants.OAuth.ACCESS_DENIED_MESSAGE
                                                               });

        var password = request.Password!.Pbkdf2HashCode(user.CreationDate.ToUnixTimeMilliseconds());

        if (user.Password != password)
            return Result<TokenResult>.UsernameOrPasswordIncorrect.Clone(new TokenResult
                                                                         {
                                                                             Error = Constants.OAuth.UNAUTHORIZED_USER,
                                                                             ErrorDescription = Constants.OAuth.UNAUTHORIZED_USER_MESSAGE
                                                                         });

        var signature = $"user:{user.Id}";

        if (_config.Caches.TryGetValue(ConfigSettings.CACHE_TOKEN_RETAIN, out var cache) && !cache.Key.IsEmpty() && cache.ServerDuration is > 0 && !signature.IsEmpty())
        {
            var cacheResult = await _redis.GetAsync<TokenResult>(cache.Key + ":" + signature);

            if (cacheResult != null)
                return Result<TokenResult>.Success.Clone(cacheResult);
        }

        var roles = user.Roles
                        .Where(t => t.ExpireDate > DateTimeOffset.UtcNow && !t.Role.Disabled)
                        .Select(t => new Role
                                     {
                                         Id = t.RoleId,
                                         Name = t.Role.Name,
                                         Priority = t.Role.Priority,
                                         ExpireDate = t.ExpireDate
                                     })
                        .OrderBy(t => t.Priority)
                        .ToArray();

        var scope = roles.Any() ? roles.Select(t => t.Id.ToString()).Aggregate((c, n) => c + " " + n) : null;

        var label = roles.Length > 0
                        ? roles[0].Name
                        : null;

        if (!TokenHelper.ScopeCheck(scope, request.Scope))
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                              Error = Constants.OAuth.INVALID_SCOPE,
                                                              ErrorDescription = string.Format(Constants.OAuth.INVALID_SCOPE_MESSAGE, request.Scope)
                                                          });

        var accessToken = _jwtGenerator.Generate(TokenType.AccessToken, ResourceType.User, user.Id.ToString(), null, user.DisplayName, user.TokenExpireSeconds, request.Scope ?? scope, label);

        var refreshToken = user.AllowedRefreshToken
                               ? _jwtGenerator.Generate(TokenType.RefreshToken, ResourceType.User, user.Id.ToString(), null, user.DisplayName, user.RefreshTokenExpireSeconds, request.Scope ?? scope, label)
                               : JwtGenerator.DefaultGenerateEmpty;

        var result = Result<TokenResult>.Success.Clone(new TokenResult
                                                       {
                                                           TokenType = Constants.OAuth.TOKEN_TYPE_BEARER,
                                                           AccessToken = accessToken.Token,
                                                           Scope = request.Scope ?? scope,
                                                           RefreshToken = refreshToken.Token,
                                                           ExpiresIn = accessToken.ExpiresIn,
                                                           NameId = user.Id.ToString(),
                                                           Roles = request.IncludeRolesInfo ? roles : null,
                                                           HasPassword = request.IncludeConfirmedInfo ? !user.Password.IsEmpty() : null,
                                                           EmailConfirmed = request.IncludeConfirmedInfo ? user.EmailConfirmed : null,
                                                           PhoneNumberConfirmed = request.IncludeConfirmedInfo ? user.PhoneNumberConfirmed : null
                                                       });

        if (cache != null && !cache.Key.IsEmpty() && cache.ServerDuration is > 0)
        {
            if (!await _redis.SetNxAsync(cache.Key + ":" + signature, result.Content!, cache.ServerDuration.Value))
            {
                var cacheResult = await _redis.GetAsync<TokenResult>(cache.Key + ":" + signature);

                if (cacheResult != null)
                    return Result<TokenResult>.Success.Clone(cacheResult);
            }
        }

        var dsToken = _context.Set<Domain.Entities.Token>();

        dsToken.Add(new Domain.Entities.Token
                    {
                        Id = _snowflake.Generate(),
                        ResourceType = ResourceType.User,
                        ResourceId = user.Id.ToString(),
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

        await _redis.PublishAsync(_config.Queues[ConfigSettings.QUEUES_USER_SIGN_IN_EVENT], "[{\"Id\":" + user.Id + ",\"LastSignInDate\":\"" + user.LastSignInDate?.ToString("O") + "\"}]");

        return result;
    }

    private async Task<Result<TokenResult>> CreateRefreshTokenAsync(CreateToken request, CancellationToken cancellationToken = default)
    {
        var dsToken = _context.Set<Domain.Entities.Token>();
        var dsClient = _context.Set<Domain.Entities.Client>();

        if (request.ClientId.IsEmpty() || !long.TryParse(request.ClientId, out var clientId) || request.ClientSecret.IsEmpty())
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                              Error = Constants.OAuth.UNAUTHORIZED_CLIENT,
                                                              ErrorDescription = Constants.OAuth.UNAUTHORIZED_CLIENT_MESSAGE
                                                          });

        if (request.RefreshToken.IsEmpty())
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                              Error = Constants.OAuth.INVALID_REQUEST,
                                                              ErrorDescription = Constants.OAuth.INVALID_REQUEST_TOKEN
                                                          });

        var signature = TokenHelper.GetJwtSignature(request.RefreshToken);

        if (_config.Caches.TryGetValue(ConfigSettings.CACHE_TOKEN_RETAIN, out var cache) && !cache.Key.IsEmpty() && cache.ServerDuration is > 0 && !signature.IsEmpty())
        {
            var cacheResult = await _redis.GetAsync<TokenResult>(cache.Key + ":" + signature);

            if (cacheResult != null)
                return Result<TokenResult>.Success.Clone(cacheResult);
        }

        if (await dsToken.AnyAsync(t => t.RefreshToken == request.RefreshToken && (t.Revoked & TokenRevoke.RefreshToken) == TokenRevoke.RefreshToken, cancellationToken))
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                              Error = Constants.OAuth.INVALID_REQUEST,
                                                              ErrorDescription = Constants.OAuth.INVALID_REQUEST_TOKEN
                                                          });

        if (!await dsClient.AnyAsync(t => t.Id == clientId, cancellationToken))
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                              Error = Constants.OAuth.UNAUTHORIZED_CLIENT,
                                                              ErrorDescription = Constants.OAuth.UNAUTHORIZED_CLIENT_MESSAGE
                                                          });

        var client = await dsClient.FirstAsync(t => t.Id == clientId, cancellationToken);

        if (client.Disabled)
            return Result<TokenResult>.Forbidden.Clone(new TokenResult
                                                       {
                                                           Error = Constants.OAuth.ACCESS_DENIED,
                                                           ErrorDescription = Constants.OAuth.ACCESS_DENIED_MESSAGE
                                                       });

        var secret = request.ClientSecret!.Pbkdf2HashCode(client.CreationDate.ToUnixTimeMilliseconds());

        if (client.Secret != secret)
            return Result<TokenResult>.Forbidden.Clone(new TokenResult
                                                       {
                                                           Error = Constants.OAuth.UNAUTHORIZED_CLIENT,
                                                           ErrorDescription = Constants.OAuth.UNAUTHORIZED_CLIENT_MESSAGE
                                                       });

        var tokenValidationParameters = _authOptions.GetTokenValidationParameters();

        ClaimsPrincipal claimsPrincipal;

        try
        {
            claimsPrincipal = TokenHelper.ValidateJwt(request.RefreshToken!, tokenValidationParameters);
        }
        catch
        {
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                              Error = Constants.OAuth.INVALID_REQUEST,
                                                              ErrorDescription = Constants.OAuth.INVALID_REQUEST_TOKEN
                                                          });
        }

        var resourceId = claimsPrincipal.Identity?.Name;
        var nickname = claimsPrincipal.FindFirst(TokenHelper.CLAIM_NICKNAME)?.Value;
        var tt = claimsPrincipal.FindFirst(TokenHelper.CLAIM_TYPES_TOKEN_TYPE)?.Value;
        var rt = claimsPrincipal.FindFirst(TokenHelper.CLAIM_TYPES_RESOURCE_TYPE)?.Value;
        var uid = claimsPrincipal.FindFirst(TokenHelper.CLAIM_UNIQUE_ID)?.Value;
        var scope = claimsPrincipal.FindFirst(_authOptions.RoleClaimType)?.Value;
        ResourceType resourceType;

        try
        {
            if (string.IsNullOrWhiteSpace(rt))
                throw new ArgumentNullException(TokenHelper.CLAIM_TYPES_RESOURCE_TYPE);

            if (!Enum.TryParse(rt, out resourceType) || (resourceType != ResourceType.Client && resourceType != ResourceType.User))
                throw new NotSupportedException("ResourceType not supported");

            if (tt == null || !Enum.TryParse(tt, out TokenType tokenType) || tokenType != TokenType.RefreshToken)
                throw new NotSupportedException("TokenType not supported");
        }
        catch
        {
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                              Error = Constants.OAuth.INVALID_REQUEST,
                                                              ErrorDescription = Constants.OAuth.INVALID_REQUEST_TOKEN
                                                          });
        }

        if (!TokenHelper.ScopeCheck(scope, request.Scope))
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                              Error = Constants.OAuth.INVALID_SCOPE,
                                                              ErrorDescription = string.Format(Constants.OAuth.INVALID_SCOPE_MESSAGE, request.Scope)
                                                          });

        try
        {
            var (disabled, roles, hasPassword, emailConfirmed, phoneNumberConfirmed, label, allowedRefreshToken, tokenExpireSeconds, refreshTokenExpireSeconds, _) = await GetResourceExpireSecondsAsync(resourceType, resourceId!);

            if (disabled)
                return Result<TokenResult>.Forbidden.Clone(new TokenResult
                                                           {
                                                               Error = Constants.OAuth.ACCESS_DENIED,
                                                               ErrorDescription = Constants.OAuth.ACCESS_DENIED_MESSAGE
                                                           });

            if (request.Scope == "*")
                scope = roles.Any() ? roles.Select(t => t.Id.ToString()).Aggregate((c, n) => c + " " + n) : null;
            else if (!string.IsNullOrWhiteSpace(request.Scope))
                scope = request.Scope;

            var accessToken = _jwtGenerator.Generate(TokenType.AccessToken, resourceType, resourceId!, uid, nickname, tokenExpireSeconds, scope, label);

            var refreshToken = allowedRefreshToken
                                   ? _jwtGenerator.Generate(TokenType.RefreshToken, resourceType, resourceId!, uid, nickname, refreshTokenExpireSeconds, scope, label)
                                   : JwtGenerator.DefaultGenerateEmpty;

            var result = Result<TokenResult>.Success.Clone(new TokenResult
                                                           {
                                                               TokenType = Constants.OAuth.TOKEN_TYPE_BEARER,
                                                               AccessToken = accessToken.Token,
                                                               Scope = scope,
                                                               RefreshToken = refreshToken.Token,
                                                               ExpiresIn = accessToken.ExpiresIn,
                                                               NameId = resourceId,
                                                               Roles = request.IncludeRolesInfo ? roles : null,
                                                               HasPassword = request.IncludeConfirmedInfo ? hasPassword : null,
                                                               EmailConfirmed = request.IncludeConfirmedInfo ? emailConfirmed : null,
                                                               PhoneNumberConfirmed = request.IncludeConfirmedInfo ? phoneNumberConfirmed : null
                                                           });

            if (cache != null && !cache.Key.IsEmpty() && cache.ServerDuration is > 0)
            {
                if (!await _redis.SetNxAsync(cache.Key + ":" + signature, result.Content!, cache.ServerDuration.Value))
                {
                    var cacheResult = await _redis.GetAsync<TokenResult>(cache.Key + ":" + signature);

                    if (cacheResult != null)
                        return Result<TokenResult>.Success.Clone(cacheResult);
                }
            }

            var token = await dsToken.FirstOrDefaultAsync(t => t.RefreshToken == request.RefreshToken && (t.Revoked & TokenRevoke.RefreshToken) != TokenRevoke.RefreshToken, cancellationToken);

            if (token != null)
            {
                var revoked = token.Revoked | TokenRevoke.RefreshToken;

                _context.Entry(token).UpdateProperty(t => t.Revoked, revoked);
            }

            dsToken.Add(new Domain.Entities.Token
                        {
                            Id = _snowflake.Generate(),
                            ResourceType = resourceType,
                            ResourceId = resourceId!,
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

            if (resourceType == ResourceType.User)
                await _redis.PublishAsync(_config.Queues[ConfigSettings.QUEUES_USER_REFRESH_TOKEN_EVENT], "[{\"Id\":" + resourceId + ",\"RefreshDate\":\"" + DateTimeOffset.UtcNow.ToString("O") + "\"}]");

            if (token == null)
                return result;

            await _redis.PublishAsync(_config.Queues[ConfigSettings.QUEUES_TOKEN_REVOKE_EVENT], await _serializer.SerializeAsync(new[] { request.RefreshToken }, cancellationToken));

            return result;
        }
        catch
        {
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                              Error = Constants.OAuth.INVALID_REQUEST,
                                                              ErrorDescription = Constants.OAuth.INVALID_REQUEST_TOKEN
                                                          });
        }
    }

    private async Task<(bool Disabled, Role[] Roles, bool? HasPassword, bool? EmailConfirmed, bool? PhoneNumberConfirmed, string? Label, bool AllowedRefreshToken, int? TokenExpireSeconds, int? RefreshTokenExpireSeconds, int? CodeExpireSeconds)> GetResourceExpireSecondsAsync(ResourceType resourceType, string resourceId)
    {
        return resourceType switch
               {
                   ResourceType.Client => await GetClientExpireSecondsAsync(resourceId),
                   ResourceType.User => await GetUserExpireSecondsAsync(resourceId),
                   _ => throw new NotSupportedException(nameof(resourceType))
               };
    }

    private async Task<(bool Disabled, Role[] Roles, bool? HasPassword, bool? EmailConfirmed, bool? PhoneNumberConfirmed, string? Label, bool AllowedRefreshToken, int? TokenExpireSeconds, int? RefreshTokenExpireSeconds, int? CodeExpireSeconds)> GetUserExpireSecondsAsync(string resourceId)
    {
        if (resourceId.IsEmpty() || !long.TryParse(resourceId, out var id)) throw new ArgumentException($"Invalid {nameof(resourceId)}.");

        var ds = _context.Set<Domain.Entities.User>();

        var entity = await ds.Include(t => t.Roles)
                             .ThenInclude(t => t.Role)
                             .FirstOrDefaultAsync(t => t.Id == id);

        if (entity == null)
            throw new Exception("Resource not found.");

        var roles = entity.Roles
                          .Where(t => t.ExpireDate > DateTimeOffset.UtcNow && !t.Role.Disabled)
                          .Select(t => new Role
                                       {
                                           Id = t.RoleId,
                                           Name = t.Role.Name,
                                           Priority = t.Role.Priority,
                                           ExpireDate = t.ExpireDate
                                       })
                          .OrderBy(t => t.Priority)
                          .ToArray();

        var label = roles.Length > 0
                        ? roles[0].Name
                        : null;

        return (entity.Disabled, roles, !entity.Password.IsEmpty(), entity.EmailConfirmed, entity.PhoneNumberConfirmed, label, entity.AllowedRefreshToken, entity.TokenExpireSeconds, entity.RefreshTokenExpireSeconds, entity.CodeExpireSeconds);
    }

    private async Task<(bool Disabled, Role[] Roles, bool? HasPassword, bool? EmailConfirmed, bool? PhoneNumberConfirmed, string? Label, bool AllowedRefreshToken, int? TokenExpireSeconds, int? RefreshTokenExpireSeconds, int? CodeExpireSeconds)> GetClientExpireSecondsAsync(string resourceId)
    {
        if (resourceId.IsEmpty() || !long.TryParse(resourceId, out var id)) throw new ArgumentException($"Invalid {nameof(resourceId)}.");

        var ds = _context.Set<Domain.Entities.Client>();

        var entity = await ds.Include(t => t.Roles)
                             .ThenInclude(t => t.Role)
                             .FirstOrDefaultAsync(t => t.Id == id);

        if (entity == null)
            throw new Exception("Resource not found.");

        var roles = entity.Roles
                          .Where(t => t.ExpireDate > DateTimeOffset.UtcNow && !t.Role.Disabled)
                          .Select(t => new Role
                                       {
                                           Id = t.RoleId,
                                           Name = t.Role.Name,
                                           Priority = t.Role.Priority,
                                           ExpireDate = t.ExpireDate
                                       })
                          .OrderBy(t => t.Priority)
                          .ToArray();

        var label = roles.Length > 0
                        ? roles[0].Name
                        : null;

        return (entity.Disabled, roles, null, null, null, label, entity.AllowedRefreshToken, entity.TokenExpireSeconds, entity.RefreshTokenExpireSeconds, entity.CodeExpireSeconds);
    }

    private Task<bool> IsValidAsync(string grantType)
    {
        var result = grantType switch
                     {
                         Constants.OAuth.GRANT_TYPE_CLIENT_CREDENTIALS => (GrantType.ClientCredentials & _authOptions.AllowedGrantType) == GrantType.ClientCredentials,
                         Constants.OAuth.GRANT_TYPE_PASSWORD => (GrantType.Password & _authOptions.AllowedGrantType) == GrantType.Password,
                         Constants.OAuth.GRANT_TYPE_REFRESH_TOKEN => (GrantType.RefreshToken & _authOptions.AllowedGrantType) == GrantType.RefreshToken,
                         _ => false
                     };

        return Task.FromResult(result);
    }
}

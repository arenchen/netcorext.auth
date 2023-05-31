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

        if (!await dsClient.AnyAsync(t => t.Id == clientId, cancellationToken))
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                                  Error = Constants.OAuth.INVALID_REQUEST,
                                                                  ErrorDescription = Constants.OAuth.INVALID_REQUEST_ID_OR_SECRET_MESSAGE
                                                          });

        var client = await dsClient.Include(t => t.Roles)
                                   .FirstAsync(t => t.Id == clientId, cancellationToken);

        if (client.Disabled)
            return Result<TokenResult>.Unauthorized.Clone(new TokenResult
                                                          {
                                                                  Error = Constants.OAuth.ACCESS_DENIED,
                                                                  ErrorDescription = Constants.OAuth.ACCESS_DENIED_MESSAGE
                                                          });

        var clientScope = client.Roles.Any() ? client.Roles.Select(t => t.RoleId.ToString()).Aggregate((c, n) => c + " " + n) : null;

        if (!TokenHelper.ScopeCheck(clientScope, request.Scope))
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                                  Error = Constants.OAuth.INVALID_SCOPE,
                                                                  ErrorDescription = string.Format(Constants.OAuth.INVALID_SCOPE_MESSAGE, request.Scope)
                                                          });

        var secret = request.ClientSecret!.Pbkdf2HashCode(client.CreationDate.ToUnixTimeMilliseconds());

        if (client.Secret != secret)
            return Result<TokenResult>.Unauthorized.Clone(new TokenResult
                                                          {
                                                                  Error = Constants.OAuth.UNAUTHORIZED_CLIENT,
                                                                  ErrorDescription = Constants.OAuth.UNAUTHORIZED_CLIENT_MESSAGE
                                                          });

        var accessToken = _jwtGenerator.Generate(TokenType.AccessToken, ResourceType.Client, client.Id.ToString(), request.UniqueId, client.TokenExpireSeconds, request.Scope ?? clientScope);

        var refreshToken = client.AllowedRefreshToken || _authOptions.AllowedRefreshToken
                                   ? _jwtGenerator.Generate(TokenType.RefreshToken, ResourceType.Client, client.Id.ToString(), request.UniqueId, client.RefreshTokenExpireSeconds, request.Scope ?? clientScope, clientScope)
                                   : (null, null);

        var result = Result<TokenResult>.Success.Clone(new TokenResult
                                                       {
                                                               TokenType = Constants.OAuth.TOKEN_TYPE_BEARER,
                                                               AccessToken = accessToken.Token,
                                                               Scope = request.Scope ?? clientScope,
                                                               RefreshToken = refreshToken.Token,
                                                               ExpiresIn = client.TokenExpireSeconds ?? _authOptions.TokenExpireSeconds
                                                       });

        var dsToken = _context.Set<Domain.Entities.Token>();

        dsToken.Add(new Domain.Entities.Token
                    {
                            Id = _snowflake.Generate(),
                            ResourceType = ResourceType.Client,
                            ResourceId = client.Id.ToString(),
                            TokenType = result.Content?.TokenType!,
                            AccessToken = result.Content?.AccessToken!,
                            ExpiresIn = result.Content?.ExpiresIn,
                            Scope = result.Content?.Scope,
                            RefreshToken = result.Content?.RefreshToken
                    });

        await _context.SaveChangesAsync(cancellationToken);

        return result;
    }

    private async Task<Result<TokenResult>> CreatePasswordCredentialsAsync(CreateToken request, CancellationToken cancellationToken = default)
    {
        if (request.ClientId.IsEmpty() || !long.TryParse(request.ClientId, out var clientId) || request.ClientSecret.IsEmpty())
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                                  Error = Constants.OAuth.INVALID_REQUEST,
                                                                  ErrorDescription = Constants.OAuth.INVALID_REQUEST_ID_OR_SECRET_MESSAGE
                                                          });

        if (request.Username.IsEmpty() || request.Password.IsEmpty())
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                                  Error = Constants.OAuth.INVALID_REQUEST,
                                                                  ErrorDescription = Constants.OAuth.INVALID_REQUEST_USERNAME_OR_PASSWORD_MESSAGE
                                                          });

        var dsClient = _context.Set<Domain.Entities.Client>();

        if (!await dsClient.AnyAsync(t => t.Id == clientId, cancellationToken))
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                                  Error = Constants.OAuth.INVALID_REQUEST,
                                                                  ErrorDescription = Constants.OAuth.INVALID_REQUEST_ID_OR_SECRET_MESSAGE
                                                          });

        var client = await dsClient.FirstAsync(t => t.Id == clientId, cancellationToken);

        if (client.Disabled)
            return Result<TokenResult>.Unauthorized.Clone(new TokenResult
                                                          {
                                                                  Error = Constants.OAuth.ACCESS_DENIED,
                                                                  ErrorDescription = Constants.OAuth.ACCESS_DENIED_MESSAGE
                                                          });

        var secret = request.ClientSecret!.Pbkdf2HashCode(client.CreationDate.ToUnixTimeMilliseconds());

        if (client.Secret != secret)
            return Result<TokenResult>.Unauthorized.Clone(new TokenResult
                                                          {
                                                                  Error = Constants.OAuth.UNAUTHORIZED_CLIENT,
                                                                  ErrorDescription = Constants.OAuth.UNAUTHORIZED_CLIENT_MESSAGE
                                                          });

        var dsUser = _context.Set<Domain.Entities.User>();

        if (!await dsUser.AnyAsync(t => t.NormalizedUsername == request.Username!.ToUpper(), cancellationToken))
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                                  Error = Constants.OAuth.INVALID_REQUEST,
                                                                  ErrorDescription = Constants.OAuth.INVALID_REQUEST_USERNAME_OR_PASSWORD_MESSAGE
                                                          });

        var user = await dsUser.Include(t => t.Roles)
                               .FirstAsync(t => t.NormalizedUsername == request.Username!.ToUpper(), cancellationToken);

        if (user.Disabled)
            return Result<TokenResult>.Unauthorized.Clone(new TokenResult
                                                          {
                                                                  Error = Constants.OAuth.ACCESS_DENIED,
                                                                  ErrorDescription = Constants.OAuth.ACCESS_DENIED_MESSAGE
                                                          });

        var userScope = user.Roles.Any(t => t.ExpireDate == null || t.ExpireDate > DateTimeOffset.UtcNow)
                                ? user.Roles.Where(t => t.ExpireDate == null || t.ExpireDate > DateTimeOffset.UtcNow).Select(t => t.RoleId.ToString()).Aggregate((c, n) => c + " " + n)
                                : null;

        if (!TokenHelper.ScopeCheck(userScope, request.Scope))
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                                  Error = Constants.OAuth.INVALID_SCOPE,
                                                                  ErrorDescription = string.Format(Constants.OAuth.INVALID_SCOPE_MESSAGE, request.Scope)
                                                          });

        var password = request.Password!.Pbkdf2HashCode(user.CreationDate.ToUnixTimeMilliseconds());

        if (user.Password != password)
            return Result<TokenResult>.Unauthorized.Clone(new TokenResult
                                                          {
                                                                  Error = Constants.OAuth.UNAUTHORIZED_USER,
                                                                  ErrorDescription = Constants.OAuth.UNAUTHORIZED_USER_MESSAGE
                                                          });

        var accessToken = _jwtGenerator.Generate(TokenType.AccessToken, ResourceType.User, user.Id.ToString(), null, user.TokenExpireSeconds, request.Scope ?? userScope);

        var refreshToken = user.AllowedRefreshToken
                                   ? _jwtGenerator.Generate(TokenType.RefreshToken, ResourceType.User, user.Id.ToString(), null, user.RefreshTokenExpireSeconds, request.Scope ?? userScope, userScope)
                                   : (null, null);

        var result = Result<TokenResult>.Success.Clone(new TokenResult
                                                       {
                                                               TokenType = Constants.OAuth.TOKEN_TYPE_BEARER,
                                                               AccessToken = accessToken.Token,
                                                               Scope = request.Scope ?? userScope,
                                                               RefreshToken = refreshToken.Token,
                                                               ExpiresIn = client.TokenExpireSeconds ?? _authOptions.TokenExpireSeconds
                                                       });

        var dsToken = _context.Set<Domain.Entities.Token>();

        dsToken.Add(new Domain.Entities.Token
                    {
                            Id = _snowflake.Generate(),
                            ResourceType = ResourceType.User,
                            ResourceId = user.Id.ToString(),
                            TokenType = result.Content?.TokenType!,
                            AccessToken = result.Content?.AccessToken!,
                            ExpiresIn = result.Content?.ExpiresIn,
                            Scope = result.Content?.Scope,
                            RefreshToken = result.Content?.RefreshToken
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
                                                                  Error = Constants.OAuth.INVALID_REQUEST,
                                                                  ErrorDescription = Constants.OAuth.INVALID_REQUEST_ID_OR_SECRET_MESSAGE
                                                          });

        if (request.RefreshToken.IsEmpty())
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                                  Error = Constants.OAuth.INVALID_REQUEST,
                                                                  ErrorDescription = Constants.OAuth.INVALID_REQUEST_TOKEN
                                                          });

        if (await dsToken.AnyAsync(t => t.Disabled && t.RefreshToken == request.RefreshToken, cancellationToken))
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                                  Error = Constants.OAuth.INVALID_REQUEST,
                                                                  ErrorDescription = Constants.OAuth.INVALID_REQUEST_TOKEN
                                                          });

        if (!await dsClient.AnyAsync(t => t.Id == clientId, cancellationToken))
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                                  Error = Constants.OAuth.INVALID_REQUEST,
                                                                  ErrorDescription = Constants.OAuth.INVALID_REQUEST_ID_OR_SECRET_MESSAGE
                                                          });

        var client = await dsClient.FirstAsync(t => t.Id == clientId, cancellationToken);

        if (client.Disabled)
            return Result<TokenResult>.Unauthorized.Clone(new TokenResult
                                                          {
                                                                  Error = Constants.OAuth.ACCESS_DENIED,
                                                                  ErrorDescription = Constants.OAuth.ACCESS_DENIED_MESSAGE
                                                          });

        var secret = request.ClientSecret!.Pbkdf2HashCode(client.CreationDate.ToUnixTimeMilliseconds());

        if (client.Secret != secret)
            return Result<TokenResult>.Unauthorized.Clone(new TokenResult
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
        var tt = claimsPrincipal.FindFirst(TokenHelper.CLAIM_TYPES_TOKEN_TYPE)?.Value;
        var rt = claimsPrincipal.FindFirst(TokenHelper.CLAIM_TYPES_RESOURCE_TYPE)?.Value;
        var uid = claimsPrincipal.FindFirst(TokenHelper.CLAIM_UNIQUE_ID)?.Value;
        var role = claimsPrincipal.FindFirst(_authOptions.RoleClaimType)?.Value;
        ResourceType resourceType;

        try
        {
            if (string.IsNullOrWhiteSpace(rt)) throw new ArgumentNullException(TokenHelper.CLAIM_TYPES_RESOURCE_TYPE);

            if (!Enum.TryParse(rt, out resourceType) || (resourceType != ResourceType.Client && resourceType != ResourceType.User)) throw new NotSupportedException("ResourceType not supported");

            if (tt == null || !Enum.TryParse(tt, out TokenType tokenType) || tokenType != TokenType.RefreshToken) throw new NotSupportedException("TokenType not supported");
        }
        catch
        {
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                                  Error = Constants.OAuth.INVALID_REQUEST,
                                                                  ErrorDescription = Constants.OAuth.INVALID_REQUEST_TOKEN
                                                          });
        }

        if (!TokenHelper.ScopeCheck(role, request.Scope))
            return Result<TokenResult>.InvalidInput.Clone(new TokenResult
                                                          {
                                                                  Error = Constants.OAuth.INVALID_SCOPE,
                                                                  ErrorDescription = string.Format(Constants.OAuth.INVALID_SCOPE_MESSAGE, request.Scope)
                                                          });

        try
        {
            var (disabled, allowedRefreshToken, tokenExpireSeconds, refreshTokenExpireSeconds, _) = await GetResourceExpireSecondsAsync(resourceType, resourceId!);

            if (disabled)
                return Result<TokenResult>.Unauthorized.Clone(new TokenResult
                                                              {
                                                                      Error = Constants.OAuth.ACCESS_DENIED,
                                                                      ErrorDescription = Constants.OAuth.ACCESS_DENIED_MESSAGE
                                                              });

            var accessToken = _jwtGenerator.Generate(TokenType.AccessToken, resourceType, resourceId!, uid, tokenExpireSeconds, request.Scope ?? role);

            var refreshToken = allowedRefreshToken
                                       ? _jwtGenerator.Generate(TokenType.RefreshToken, resourceType, resourceId!, uid, refreshTokenExpireSeconds, request.Scope ?? role, role)
                                       : (null, null);

            var result = Result<TokenResult>.Success.Clone(new TokenResult
                                                           {
                                                                   TokenType = Constants.OAuth.TOKEN_TYPE_BEARER,
                                                                   AccessToken = accessToken.Token,
                                                                   Scope = request.Scope ?? role,
                                                                   RefreshToken = refreshToken.Token,
                                                                   ExpiresIn = client.TokenExpireSeconds ?? _authOptions.TokenExpireSeconds
                                                           });

            var token = await dsToken.FirstOrDefaultAsync(t => !t.Disabled && t.RefreshToken == request.RefreshToken, cancellationToken);

            if (token != null)
            {
                token.Disabled = true;

                _context.Entry(token).Property(t => t.Disabled).IsModified = true;
            }

            dsToken.Add(new Domain.Entities.Token
                        {
                                Id = _snowflake.Generate(),
                                ResourceType = resourceType,
                                ResourceId = resourceId!,
                                TokenType = result.Content?.TokenType!,
                                AccessToken = result.Content?.AccessToken!,
                                ExpiresIn = result.Content?.ExpiresIn,
                                Scope = result.Content?.Scope,
                                RefreshToken = result.Content?.RefreshToken
                        });

            await _context.SaveChangesAsync(cancellationToken);

            if (resourceType == ResourceType.User)
                await _redis.PublishAsync(_config.Queues[ConfigSettings.QUEUES_USER_REFRESH_TOKEN_EVENT], "[{\"Id\":" + resourceId + ",\"RefreshDate\":\"" + DateTimeOffset.UtcNow.ToString("O") + "\"}]");

            if (token == null) return result;

            var lsToken = new List<string> { token.AccessToken };

            if (!string.IsNullOrWhiteSpace(token.RefreshToken)) lsToken.Add(token.RefreshToken);

            await _redis.PublishAsync(_config.Queues[ConfigSettings.QUEUES_TOKEN_REVOKE_EVENT], await _serializer.SerializeAsync(lsToken.ToArray(), cancellationToken));

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

    private async Task<(bool Disabled, bool AllowedRefreshToken, int? TokenExpireSeconds, int? RefreshTokenExpireSeconds, int? CodeExpireSeconds)> GetResourceExpireSecondsAsync(ResourceType resourceType, string resourceId)
    {
        return resourceType switch
               {
                       ResourceType.Client => await GetClientExpireSecondsAsync(resourceId),
                       ResourceType.User => await GetUserExpireSecondsAsync(resourceId),
                       _ => throw new NotSupportedException(nameof(resourceType))
               };
    }

    private async Task<(bool Disabled, bool AllowedRefreshToken, int? TokenExpireSeconds, int? RefreshTokenExpireSeconds, int? CodeExpireSeconds)> GetUserExpireSecondsAsync(string resourceId)
    {
        if (resourceId.IsEmpty() || !long.TryParse(resourceId, out var id)) throw new ArgumentException($"Invalid {nameof(resourceId)}.");

        var ds = _context.Set<Domain.Entities.User>();

        var entity = await ds.FirstOrDefaultAsync(t => t.Id == id);

        if (entity == null) throw new Exception("Resource not found.");

        return (entity.Disabled, entity.AllowedRefreshToken, entity.TokenExpireSeconds, entity.RefreshTokenExpireSeconds, entity.CodeExpireSeconds);
    }

    private async Task<(bool Disabled, bool AllowedRefreshToken, int? TokenExpireSeconds, int? RefreshTokenExpireSeconds, int? CodeExpireSeconds)> GetClientExpireSecondsAsync(string resourceId)
    {
        if (resourceId.IsEmpty() || !long.TryParse(resourceId, out var id)) throw new ArgumentException($"Invalid {nameof(resourceId)}.");

        var ds = _context.Set<Domain.Entities.Client>();

        var entity = await ds.FirstOrDefaultAsync(t => t.Id == id);

        if (entity == null) throw new Exception("Resource not found.");

        return (entity.Disabled, entity.AllowedRefreshToken, entity.TokenExpireSeconds, entity.RefreshTokenExpireSeconds, entity.CodeExpireSeconds);
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
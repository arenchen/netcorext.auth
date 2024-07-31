using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Netcorext.Auth.Authentication.Extensions;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Auth.Extensions;
using Netcorext.Auth.Helpers;
using Netcorext.Contracts;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Hash;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Middlewares;

internal class TokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TokenMiddleware> _logger;
    private readonly ConfigSettings _config;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public TokenMiddleware(RequestDelegate next,
                           IMemoryCache cache,
                           IOptions<AuthOptions> authOptions,
                           IOptions<ConfigSettings> config,
                           ILogger<TokenMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _config = config.Value;
        _tokenValidationParameters = authOptions.Value.GetTokenValidationParameters();
    }

    public async Task InvokeAsync(HttpContext context, IDispatcher dispatcher)
    {
        if (_config.AppSettings.InternalHost?.Any(t => t.Equals(context.Request.Host.Host, StringComparison.CurrentCultureIgnoreCase)) ?? false)
        {
            await _next(context);

            return;
        }

        var headerValue = context.Request.Headers["Authorization"];

        var result = await IsValid(dispatcher, headerValue);

        if (result == Result.Success)
        {
            await _next(context);

            return;
        }

        if (result == Result.UnauthorizedAndCannotRefreshToken)
            await context.UnauthorizedAsync(_config.AppSettings.UseNativeStatus, result.Code);
        else if (result == Result.Forbidden)
            await context.ForbiddenAsync(_config.AppSettings.UseNativeStatus);
        else if (result == Result.AccountIsDisabled)
            await context.ForbiddenAsync(_config.AppSettings.UseNativeStatus, result.Code);
        else
            await context.UnauthorizedAsync(_config.AppSettings.UseNativeStatus);
    }

    private async Task<Result> IsValid(IDispatcher dispatcher, string headerValue)
    {
        if (headerValue.IsEmpty())
        {
            _logger.LogDebug("Unauthorized, no authorization header");

            return Result.Success;
        }

        if (!AuthenticationHeaderValue.TryParse(headerValue, out var authHeader))
        {
            _logger.LogWarning("Unauthorized, header 'Authorization' is invalid");

            return Result.Unauthorized;
        }

        var token = authHeader.Parameter;

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Token is empty");

            return Result.Unauthorized;
        }

        var result = authHeader.Scheme.ToUpper() switch
                     {
                         Constants.OAuth.TOKEN_TYPE_BASIC_NORMALIZED => await IsBasicValid(token),
                         Constants.OAuth.TOKEN_TYPE_BEARER_NORMALIZED => await IsBearerValid(token),
                         _ => Result.Unauthorized
                     };

        if (result.Code != Result.Success)
            _logger.LogWarning("Unauthorized, token is invalid");

        return result;
    }

    private Task<Result> IsBasicValid(string token)
    {
        if (_cache.TryGetValue(token, out Result cacheResult))
            return Task.FromResult(cacheResult);

        var raw = Encoding.UTF8.GetString(Convert.FromBase64String(token));

        var client = raw.Split(":", StringSplitOptions.RemoveEmptyEntries);

        if (client.Length != 2 || !long.TryParse(client[0], out var clientId) || string.IsNullOrWhiteSpace(client[1]))
            return Task.FromResult(Result.Unauthorized);

        var cacheBlockedClient = _cache.Get<HashSet<long>>(ConfigSettings.CACHE_BLOCKED_CLIENT) ?? new HashSet<long>();

        if (cacheBlockedClient.Contains(clientId))
        {
            _cache.Set(token, Result.AccountIsDisabled, DateTimeOffset.UtcNow.AddMilliseconds(_config.AppSettings.CacheTokenExpires));

            return Task.FromResult(Result.AccountIsDisabled);
        }

        var cacheClient = _cache.Get<Dictionary<long, Netcorext.Auth.Authentication.Services.Client.Queries.Models.Client>>(ConfigSettings.CACHE_CLIENT) ?? new Dictionary<long, Netcorext.Auth.Authentication.Services.Client.Queries.Models.Client>();

        if (!cacheClient.TryGetValue(clientId, out var entity) || entity.Secret != client[1].Pbkdf2HashCode(entity.CreationDate.ToUnixTimeMilliseconds()))
        {
            _cache.Set(token, Result.UsernameOrPasswordIncorrect, DateTimeOffset.UtcNow.AddMilliseconds(_config.AppSettings.CacheTokenExpires));

            return Task.FromResult(Result.UsernameOrPasswordIncorrect);
        }


        _cache.Set(token, Result.Success, DateTimeOffset.UtcNow.AddMilliseconds(_config.AppSettings.CacheTokenExpires));

        return Task.FromResult(Result.Success);
    }

    private Task<Result> IsBearerValid(string token)
    {
        try
        {
            if (_cache.TryGetValue(token, out Result cacheResult))
                return Task.FromResult(cacheResult);

            var claimsPrincipal = TokenHelper.ValidateJwt(token, _tokenValidationParameters);

            var claimName = claimsPrincipal.Identity?.Name;
            var tt = claimsPrincipal.Claims.FirstOrDefault(t => t.Type == TokenHelper.CLAIM_TYPES_TOKEN_TYPE)?.Value;
            var rt = claimsPrincipal.Claims.FirstOrDefault(t => t.Type == TokenHelper.CLAIM_TYPES_RESOURCE_TYPE)?.Value;

            if (tt != "1" || string.IsNullOrWhiteSpace(claimName) || !long.TryParse(claimName, out var id))
                return Task.FromResult(Result.InvalidInput);
            if (rt != "0" && rt != "1")
                return Task.FromResult(Result.InvalidInput);

            if (rt == "0")
            {
                var cacheBlockedClient = _cache.Get<HashSet<long>>(ConfigSettings.CACHE_BLOCKED_CLIENT) ?? new HashSet<long>();

                if (cacheBlockedClient.Contains(id))
                {
                    _cache.Set(token, Result.AccountIsDisabled, DateTimeOffset.UtcNow.AddMilliseconds(_config.AppSettings.CacheTokenExpires));

                    return Task.FromResult(Result.AccountIsDisabled);
                }
            }
            else
            {
                var cacheBlockedUser = _cache.Get<HashSet<long>>(ConfigSettings.CACHE_BLOCKED_USER) ?? new HashSet<long>();

                if (cacheBlockedUser.Contains(id))
                {
                    _cache.Set(token, Result.AccountIsDisabled, DateTimeOffset.UtcNow.AddMilliseconds(_config.AppSettings.CacheTokenExpires));

                    return Task.FromResult(Result.AccountIsDisabled);
                }
            }

            _cache.Set(token, Result.Success, DateTimeOffset.UtcNow.AddMilliseconds(_config.AppSettings.CacheTokenExpires));

            return Task.FromResult(Result.Success);
        }
        catch (SecurityTokenExpiredException)
        {
            _cache.Remove(token);

            return Task.FromResult(Result.Unauthorized);
        }
        catch
        {
            return Task.FromResult(Result.Unauthorized);
        }
    }
}

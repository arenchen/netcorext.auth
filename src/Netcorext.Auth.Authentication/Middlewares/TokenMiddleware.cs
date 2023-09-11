using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Netcorext.Auth.Authentication.Extensions;
using Netcorext.Auth.Authentication.Services.Client.Queries;
using Netcorext.Auth.Authentication.Services.Token.Queries;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Auth.Extensions;
using Netcorext.Auth.Helpers;
using Netcorext.Contracts;
using Netcorext.Extensions.Commons;
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

        await context.UnauthorizedAsync(_config.AppSettings.UseNativeStatus);
    }

    private async Task<Result> IsValid(IDispatcher dispatcher, string headerValue)
    {
        if (headerValue.IsEmpty())
        {
            _logger.LogWarning("Unauthorized, no authorization header");

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
                         Constants.OAuth.TOKEN_TYPE_BASIC_NORMALIZED => await IsBasicValid(dispatcher, token),
                         Constants.OAuth.TOKEN_TYPE_BEARER_NORMALIZED => await IsBearerValid(dispatcher, token),
                         _ => Result.Unauthorized
                     };

        if (result.Code != Result.Success)
            _logger.LogWarning("Unauthorized, token is invalid");

        return result;
    }

    private async Task<Result> IsBasicValid(IDispatcher dispatcher, string token)
    {
        var raw = Encoding.UTF8.GetString(Convert.FromBase64String(token));

        var client = raw.Split(":", StringSplitOptions.RemoveEmptyEntries);

        if (client.Length != 2 || !long.TryParse(client[0], out var clientId))
            return Result.Unauthorized;

        var result = await dispatcher.SendAsync(new ValidateClient
                                                {
                                                    Id = clientId,
                                                    Secret = client[1]
                                                });

        return result;
    }

    private async Task<Result> IsBearerValid(IDispatcher dispatcher, string token)
    {
        try
        {
            TokenHelper.ValidateJwt(token, _tokenValidationParameters);

            if (_cache.TryGetValue(token, out Result cacheResult)) return cacheResult;

            var result = await dispatcher.SendAsync(new ValidateToken
                                                    {
                                                        Token = token
                                                    });

            _cache.Set(token, result, DateTimeOffset.UtcNow.AddMilliseconds(_config.AppSettings.CacheTokenExpires));

            return result;
        }
        catch (SecurityTokenExpiredException)
        {
            _cache.Remove(token);

            return Result.Unauthorized;
        }
        catch
        {
            return Result.Unauthorized;
        }
    }
}

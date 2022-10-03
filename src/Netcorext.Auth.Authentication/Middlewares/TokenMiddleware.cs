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
    private readonly ConfigSettings _config;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private IDispatcher _dispatcher;

    public TokenMiddleware(RequestDelegate next,
                           IDispatcher dispatcher,
                           IMemoryCache cache,
                           IOptions<AuthOptions> authOptions,
                           IOptions<ConfigSettings> config)
    {
        _next = next;
        _dispatcher = dispatcher;
        _cache = cache;
        _config = config.Value;
        _tokenValidationParameters = authOptions.Value.GetTokenValidationParameters();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _dispatcher = context.RequestServices.GetRequiredService<IDispatcher>();

        if (_config.AppSettings.InternalHost?.Any(t => t.Equals(context.Request.Host.Host, StringComparison.CurrentCultureIgnoreCase)) ?? false)
        {
            await _next(context);

            return;
        }

        var headerValue = context.Request.Headers["Authorization"];

        if (await IsValid(headerValue))
        {
            await _next(context);

            return;
        }

        await context.UnauthorizedAsync(_config.AppSettings.UseNativeStatus);
    }

    private async Task<bool> IsValid(string headerValue)
    {
        if (headerValue.IsEmpty()) return true;

        if (!AuthenticationHeaderValue.TryParse(headerValue, out var authHeader)) return false;

        var token = authHeader.Parameter;

        if (string.IsNullOrWhiteSpace(token)) return false;

        return authHeader.Scheme.ToUpper() switch
               {
                   Constants.OAuth.TOKEN_TYPE_BASIC_NORMALIZED => await IsBasicValid(token),
                   Constants.OAuth.TOKEN_TYPE_BEARER_NORMALIZED => await IsBearerValid(token),
                   _ => false
               };
    }

    private async Task<bool> IsBasicValid(string token)
    {
        var raw = Encoding.UTF8.GetString(Convert.FromBase64String(token));

        var client = raw.Split(":", StringSplitOptions.RemoveEmptyEntries);

        if (client.Length != 2 || !long.TryParse(client[0], out var clientId)) return false;

        var result = await _dispatcher.SendAsync(new ValidateClient
                                                 {
                                                     Id = clientId,
                                                     Secret = client[1]
                                                 });

        return result != null && result.Code == Result.Success;
    }

    private async Task<bool> IsBearerValid(string token)
    {
        var cachePermissions = _cache.Get<Dictionary<string, bool>>(ConfigSettings.CACHE_TOKEN) ?? new Dictionary<string, bool>();

        try
        {
            TokenHelper.ValidateJwt(token, _tokenValidationParameters);

            if (cachePermissions.TryGetValue(token, out var cacheResult)) return cacheResult;

            var result = await _dispatcher.SendAsync(new ValidateToken
                                                     {
                                                         Token = token
                                                     });

            if (!cachePermissions.TryAdd(token, result != null && result.Code == Result.Success))
            {
                cachePermissions[token] = result != null && result.Code == Result.Success;
            }

            return result != null && result.Code == Result.Success;
        }
        catch (SecurityTokenExpiredException e)
        {
            cachePermissions.Remove(token);

            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            _cache.Set(ConfigSettings.CACHE_TOKEN, cachePermissions, DateTimeOffset.UtcNow.AddMilliseconds(_config.AppSettings.CacheTokenExpires));
        }
    }
}
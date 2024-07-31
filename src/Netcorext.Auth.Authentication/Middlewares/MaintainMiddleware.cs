using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Extensions;
using Netcorext.Auth.Authentication.Services.Maintenance.Queries.Models;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Middlewares;

internal class MaintainMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MaintainMiddleware> _logger;
    private readonly ConfigSettings _config;

    public MaintainMiddleware(RequestDelegate next, IMemoryCache cache, IOptions<ConfigSettings> config, ILogger<MaintainMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _config = config.Value;
    }

    public async Task InvokeAsync(HttpContext context, IDispatcher dispatcher)
    {
        var host = GetHost(context);

        if (_config.AppSettings.InternalHost?.Any(t => t.Equals(host, StringComparison.CurrentCultureIgnoreCase)) ?? false)
        {
            await _next(context);

            return;
        }

        if (!_cache.TryGetValue<IDictionary<string, MaintainItem>>($"{ConfigSettings.CACHE_MAINTAIN}", out var maintain))
        {
            await _next(context);

            return;
        }

        var claimName = context.User.Identity?.Name;

        if (long.TryParse(claimName, out var id) && (_config.AppSettings.Owner?.Any(t => t == id) ?? false))
        {
            await _next(context);

            return;
        }

        var isMaintain = false;
        var key = string.Empty;
        var message = string.Empty;

        var role = context.User.Claims.FirstOrDefault(t => t.Type == ClaimTypes.Role)?.Value;
        var roles = (role?.Split() ?? Array.Empty<string>()).Select(long.Parse);

        foreach (var item in maintain)
        {
            if (item.Value.BeginDate > DateTimeOffset.Now || item.Value.EndDate < DateTimeOffset.Now)
            {
                continue;
            }
            if (item.Value.ExcludeHosts != null && item.Value.ExcludeHosts.Any(h => h.Equals(host, StringComparison.CurrentCultureIgnoreCase)))
            {
                continue;
            }
            if (item.Value.ExcludeRoles != null && item.Value.ExcludeRoles.Any(t => roles.Contains(t)))
            {
                continue;
            }

            isMaintain = true;
            key = item.Key;
            message = item.Value.Message;
        }

        if (!isMaintain)
        {
            await _next(context);

            return;
        }

        _logger.LogInformation("Service Unavailable: {Key}, {Message}", key, message);

        await context.ServiceUnavailableAsync(_config.AppSettings.UseNativeStatus, message: message);
    }

    private static string GetHost(HttpContext context)
    {
        var host = context.Request.Host.Host;

        if (context.Request.Headers.TryGetValue("X-Forwarded-Host", out var values))
        {
            host = values.FirstOrDefault() ?? host;
        }

        return host;
    }
}

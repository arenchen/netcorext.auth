using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Extensions;
using Netcorext.Auth.Authentication.Services.Maintenance.Commands;
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
        if (!_cache.TryGetValue<Maintain>(ConfigSettings.CACHE_MAINTAIN, out var maintain) || !maintain.Enabled)
        {
            await _next(context);

            return;
        }

        var host = GetHost(context);

        if (_config.AppSettings.InternalHost?.Any(t => t.Equals(host, StringComparison.CurrentCultureIgnoreCase)) ?? false)
        {
            await _next(context);

            return;
        }

        var claimName = context.User.Claims.FirstOrDefault(t => t.Type == ClaimTypes.Name)?.Value;

        if (long.TryParse(claimName, out var id) && (_config.AppSettings.Owner?.Any(t => t == id) ?? false))
        {
            await _next(context);

            return;
        }

        if (maintain.ExcludeHosts != null && maintain.ExcludeHosts.Any(t => t.Equals(host, StringComparison.CurrentCultureIgnoreCase)))
        {
            await _next(context);

            return;
        }

        var role = context.User.Claims.FirstOrDefault(t => t.Type == ClaimTypes.Role)?.Value;

        var roles = (role?.Split() ?? Array.Empty<string>()).Select(long.Parse);

        if (maintain.ExcludeRoles?.Any() == true && roles.Any(t => maintain.ExcludeRoles.Contains(t)))
        {
            await _next(context);

            return;
        }

        _logger.LogInformation("Service Unavailable: {Message}", maintain.Message);

        await context.ServiceUnavailableAsync(_config.AppSettings.UseNativeStatus, message: maintain.Message);
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

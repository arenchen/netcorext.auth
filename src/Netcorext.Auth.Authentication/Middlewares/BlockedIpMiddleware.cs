using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Extensions;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Auth.Extensions;
using Netcorext.Auth.Helpers;

namespace Netcorext.Auth.Authentication.Middlewares;

public class BlockedIpMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ConfigSettings _config;
    private readonly ILogger<BlockedIpMiddleware> _logger;

    public BlockedIpMiddleware(RequestDelegate next, IMemoryCache cache, IOptions<ConfigSettings> config, ILogger<BlockedIpMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _config = config.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_config.AppSettings.InternalHost?.Any(t => t.Equals(context.Request.Host.Host, StringComparison.CurrentCultureIgnoreCase)) ?? false)
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

        var ip = context.GetIp() ?? "0.0.0.0";
        var ipNumber = IpHelper.ConvertToNumber(ip);
        var cacheBlockedIp = _cache.Get<Dictionary<long, Services.Blocked.Queries.Models.BlockedIp>>(ConfigSettings.CACHE_BLOCKED_IP) ?? new Dictionary<long, Services.Blocked.Queries.Models.BlockedIp>();

        if (cacheBlockedIp.Any(t => t.Value.BeginRange >= ipNumber && t.Value.EndRange <= ipNumber))
        {
            _logger.LogWarning("Forbidden, This specified ip is blocked: {Ip}", ip);

            await context.ForbiddenAsync(_config.AppSettings.UseNativeStatus, "403008");
        }

        await _next(context);
    }
}

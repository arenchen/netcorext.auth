using System.Diagnostics;
using Netcorext.Auth.Extensions;


namespace Netcorext.Auth.Authentication.Middlewares;

public class TrafficMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TrafficMiddleware> _logger;
    private readonly ILogger _loggerTraffic;

    public TrafficMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
    {
        _next = next;
        _logger = loggerFactory.CreateLogger<TrafficMiddleware>();
        _loggerTraffic = loggerFactory.CreateLogger($"Auth-{nameof(Models.Traffic)}");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var watcher = Stopwatch.StartNew();

        await _next(context);

        watcher.Stop();

        try
        {
            var traffic = new Models.Traffic
                          {
                              TrafficDate = DateTimeOffset.UtcNow,
                              Protocol = context.Request.Protocol,
                              Scheme = context.Request.Scheme,
                              Method = context.Request.Method,
                              Url = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}",
                              Host = context.Request.Host.Value,
                              Path = context.Request.Path,
                              QueryString = context.Request.QueryString.Value,
                              Headers = context.Request.Headers.GetHeadersString(),
                              ResponseHeaders = context.Response.Headers.GetHeadersString(),
                              Ip = context.GetIp(),
                              TraceIdentifier = context.TraceIdentifier,
                              StatusCode = context.Response.StatusCode.ToString(),
                              Elapsed = watcher.Elapsed,
                              DeviceId = context.Request.Headers.GetDeviceId(),
                              XRequestId = context.Request.Headers.GetRequestId(),
                              User = context.User.GetUser(),
                              UserAgent = context.Request.Headers.GetUserAgent()
                          };

            _loggerTraffic.LogTrace("{@Traffic}", traffic);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
        }
    }
}

using System.Diagnostics;
using Microsoft.Extensions.Primitives;
using Netcorext.Auth.Extensions;


namespace Netcorext.Auth.Authentication.Middlewares;

public class TrafficMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Models.TrafficQueue _trafficQueue;
    private readonly ILogger<TrafficMiddleware> _logger;

    public TrafficMiddleware(RequestDelegate next, Models.TrafficQueue trafficQueue, ILogger<TrafficMiddleware> logger)
    {
        _next = next;
        _trafficQueue = trafficQueue;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {

        await _next(context);

        try
        {
            var traffic = new Models.TrafficRaw
                          {
                              Timestamp = DateTimeOffset.UtcNow,
                              Protocol = context.Request.Protocol,
                              Scheme = context.Request.Scheme,
                              HttpMethod = context.Request.Method,
                              Url = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}",
                              Host = context.Request.Host.Value,
                              Path = context.Request.Path,
                              QueryString = context.Request.QueryString.Value,
                              Headers = new HeaderDictionary(context.Request.Headers.ToDictionary(t => t.Key, t => t.Value)),
                              ResponseHeaders = new HeaderDictionary(context.Response.Headers.ToDictionary(t => t.Key, t => t.Value)),
                              Ip = context.GetIp(),
                              TraceIdentifier = context.TraceIdentifier,
                              Status = context.Response.StatusCode.ToString(),
                              User = context.User
                          };

            _trafficQueue.Enqueue(traffic);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
        }
    }
}

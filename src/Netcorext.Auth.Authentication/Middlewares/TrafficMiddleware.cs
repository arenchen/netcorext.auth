using FreeRedis;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Auth.Extensions;

namespace Netcorext.Auth.Authentication.Middlewares;

public class TrafficMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RedisClient _redis;
    private readonly ILogger<TrafficMiddleware> _logger;
    private readonly ConfigSettings _config;

    public TrafficMiddleware(RequestDelegate next, RedisClient redis, IOptions<ConfigSettings> config, ILogger<TrafficMiddleware> logger)
    {
        _next = next;
        _redis = redis;
        _logger = logger;
        _config = config.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        try
        {
            var traffic = new Models.Traffic
                          {
                              Timestamp = DateTimeOffset.UtcNow,
                              Protocol = context.Request.Protocol,
                              Scheme = context.Request.Scheme,
                              HttpMethod = context.Request.Method,
                              Url = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}",
                              Host = context.Request.Host.Value,
                              Path = context.Request.Path,
                              QueryString = context.Request.QueryString.Value,
                              Headers = context.GetRequestHeaders(),
                              ResponseHeaders = context.GetResponseHeaders(),
                              DeviceId = context.GetDeviceId(),
                              Ip = context.GetIp(),
                              TraceIdentifier = context.TraceIdentifier,
                              XRequestId = context.GetRequestId(),
                              Status = context.Response.StatusCode.ToString(),
                              UserAgent = context.GetUserAgent(),
                              User = context.GetUser()
                          };

            var channelKey = _config.Queues[ConfigSettings.QUEUES_TRAFFIC_EVENT];
            var streamKey = _config.Queues[ConfigSettings.QUEUES_TRAFFIC];

            var values = new Dictionary<string, object>
                         {
                             { "Timestamp", traffic.Timestamp.ToUnixTimeMilliseconds() },
                             { "Data", traffic }
                         };

            await _redis.XAddAsync(streamKey, _config.AppSettings.StreamMaxLength, "*", values);

            await _redis.PublishAsync(channelKey, streamKey);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
        }
    }
}

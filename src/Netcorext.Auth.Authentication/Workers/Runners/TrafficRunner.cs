using FreeRedis;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Auth.Extensions;
using Netcorext.Auth.Models;
using Netcorext.Worker;

namespace Netcorext.Auth.Authentication.Workers;

internal class TrafficRunner : IWorkerRunner<AuthWorker>
{
    private readonly RedisClient _redis;
    private readonly TrafficQueue _trafficQueue;
    private readonly ILogger<TrafficRunner> _logger;
    private readonly ConfigSettings _config;

    public TrafficRunner(RedisClient redis, TrafficQueue trafficQueue, IOptions<ConfigSettings> config, ILogger<TrafficRunner> logger)
    {
        _redis = redis;
        _trafficQueue = trafficQueue;
        _logger = logger;
        _config = config.Value;
    }

    public Task InvokeAsync(AuthWorker worker, CancellationToken cancellationToken = default)
    {
        var channelKey = _config.Queues[ConfigSettings.QUEUES_TRAFFIC_EVENT];
        var streamKey = _config.Queues[ConfigSettings.QUEUES_TRAFFIC];

        _trafficQueue.TrafficEnqueued += async (sender, args) =>
                                         {
                                             try
                                             {
                                                 while (_trafficQueue.TryDequeue(out var rawTraffic))
                                                 {
                                                     if (rawTraffic == null)
                                                         continue;

                                                     var traffic = new Traffic
                                                                   {
                                                                       Timestamp = rawTraffic.Timestamp,
                                                                       Protocol = rawTraffic.Protocol,
                                                                       Scheme = rawTraffic.Scheme,
                                                                       HttpMethod = rawTraffic.HttpMethod,
                                                                       Url = rawTraffic.Url,
                                                                       Host = rawTraffic.Host,
                                                                       Path = rawTraffic.Path,
                                                                       QueryString = rawTraffic.QueryString,
                                                                       Headers = rawTraffic.Headers?.GetHeadersString(),
                                                                       ResponseHeaders = rawTraffic.ResponseHeaders?.GetHeadersString(),
                                                                       Ip = rawTraffic.Ip,
                                                                       TraceIdentifier = rawTraffic.TraceIdentifier,
                                                                       Status = rawTraffic.Status,
                                                                       DeviceId = rawTraffic.Headers?.GetDeviceId(),
                                                                       XRequestId = rawTraffic.Headers?.GetRequestId(),
                                                                       User = rawTraffic.User?.GetUser(),
                                                                       UserAgent = rawTraffic.Headers?.GetUserAgent()
                                                                   };

                                                     var values = new Dictionary<string, object>
                                                                  {
                                                                      { "Timestamp", traffic.Timestamp.ToUnixTimeMilliseconds() },
                                                                      { "Data", traffic }
                                                                  };

                                                     try
                                                     {
                                                         await _redis.XAddAsync(streamKey, _config.AppSettings.StreamMaxLength, "*", values);
                                                     }
                                                     catch (Exception e)
                                                     {
                                                         _logger.LogError(e, "{Message}", e.Message);
                                                     }
                                                 }

                                                 await _redis.PublishAsync(channelKey, streamKey);
                                             }
                                             catch (Exception e)
                                             {
                                                 _logger.LogError(e, "{Message}", e.Message);
                                             }
                                         };

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _trafficQueue.Dispose();
    }
}

using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Services.Blocked.Queries;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Contracts;
using Netcorext.Extensions.Linq;
using Netcorext.Extensions.Threading;
using Netcorext.Mediator;
using Netcorext.Serialization;
using Netcorext.Worker;

namespace Netcorext.Auth.Authentication.Workers;

internal class BlockedIpRunner : IWorkerRunner<AuthWorker>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RedisClient _redis;
    private readonly IMemoryCache _cache;
    private readonly ISerializer _serializer;
    private readonly ConfigSettings _config;
    private readonly ILogger<BlockedIpRunner> _logger;
    private IDisposable? _subscriber;
    private static readonly KeyLocker Locker = new KeyLocker();

    public BlockedIpRunner(IServiceProvider serviceProvider, RedisClient redis, IMemoryCache cache, ISerializer serializer, IOptions<ConfigSettings> config, ILogger<BlockedIpRunner> logger)
    {
        _serviceProvider = serviceProvider;
        _redis = redis;
        _cache = cache;
        _serializer = serializer;
        _config = config.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(AuthWorker worker, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{Message}", nameof(BlockedIpRunner));

        _subscriber?.Dispose();

        _subscriber = _redis.Subscribe(_config.Queues[ConfigSettings.QUEUES_BLOCKED_IP_CHANGE_EVENT], Handler);

        await UpdateBlockedIpAsync(null, cancellationToken);

        return;

        async void Handler(string s, object o)
        {
            await UpdateBlockedIpAsync(o.ToString(), cancellationToken);
        }
    }

    private async Task UpdateBlockedIpAsync(string? ids, CancellationToken cancellationToken = default)
    {
        try
        {
            await Locker.WaitAsync(nameof(UpdateBlockedIpAsync), cancellationToken);

            _logger.LogInformation(nameof(UpdateBlockedIpAsync));

            using var scope = _serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            var reqIds = ids == null ? null : _serializer.Deserialize<long[]>(ids);

            var result = await dispatcher.SendAsync(new GetBlockedIp
                                                    {
                                                        Ids = reqIds
                                                    }, cancellationToken);

            if (result.Content == null || result.Code != Result.Success) return;

            var cacheBlockedIp = _cache.Get<Dictionary<long, Services.Blocked.Queries.Models.BlockedIp>>(ConfigSettings.CACHE_BLOCKED_IP) ?? new Dictionary<long, Services.Blocked.Queries.Models.BlockedIp>();

            if (reqIds != null && reqIds.Any())
            {
                var rules = cacheBlockedIp.Where(t => reqIds.Contains(t.Value.Id))
                                          .ToArray();

                rules.ForEach(t => cacheBlockedIp.Remove(t.Key));
            }

            foreach (var i in result.Content)
            {
                var id = i.Id;

                if (cacheBlockedIp.TryAdd(id, i)) continue;

                cacheBlockedIp[id] = i;
            }

            _cache.Set(ConfigSettings.CACHE_BLOCKED_IP, cacheBlockedIp);
        }
        finally
        {
            Locker.Release(nameof(UpdateBlockedIpAsync));
        }
    }

    public void Dispose()
    {
        _subscriber?.Dispose();
    }
}

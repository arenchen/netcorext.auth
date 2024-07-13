using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Services.Permission.Queries;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Contracts;
using Netcorext.Extensions.Linq;
using Netcorext.Extensions.Threading;
using Netcorext.Mediator;
using Netcorext.Serialization;
using Netcorext.Worker;

namespace Netcorext.Auth.Authentication.Workers;

internal class PermissionRunner : IWorkerRunner<AuthWorker>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RedisClient _redis;
    private readonly IMemoryCache _cache;
    private readonly ISerializer _serializer;
    private readonly ConfigSettings _config;
    private readonly ILogger<PermissionRunner> _logger;
    private IDisposable? _subscriber;
    private static readonly KeyLocker Locker = new KeyLocker();

    public PermissionRunner(IServiceProvider serviceProvider, RedisClient redis, IMemoryCache cache, ISerializer serializer, IOptions<ConfigSettings> config, ILogger<PermissionRunner> logger)
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
        _logger.LogDebug("{Message}", nameof(RoleRunner));

        _subscriber?.Dispose();

        _subscriber = _redis.Subscribe(_config.Queues[ConfigSettings.QUEUES_PERMISSION_CHANGE_EVENT], Handler);

        await UpdatePermissionAsync(null, cancellationToken);

        return;

        async void Handler(string s, object o)
        {
            await UpdatePermissionAsync(o.ToString(), cancellationToken);
        }
    }

    private async Task UpdatePermissionAsync(string? ids, CancellationToken cancellationToken = default)
    {
        try
        {
            await Locker.WaitAsync(nameof(UpdatePermissionAsync), cancellationToken);

            _logger.LogInformation(nameof(UpdatePermissionAsync));

            using var scope = _serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            var reqIds = ids == null ? null : _serializer.Deserialize<long[]>(ids);

            var result = await dispatcher.SendAsync(new GetPermission
                                                    {
                                                        Ids = reqIds
                                                    }, cancellationToken);

            if (result.Content == null || result.Code != Result.Success) return;

            var cachePermissionRule = _cache.Get<Dictionary<long, Services.Permission.Queries.Models.PermissionRule>>(ConfigSettings.CACHE_PERMISSION_RULE) ?? new Dictionary<long, Services.Permission.Queries.Models.PermissionRule>();

            if (reqIds != null && reqIds.Any())
            {
                var rules = cachePermissionRule.Where(t => reqIds.Contains(t.Value.Id))
                                               .ToArray();

                rules.ForEach(t => cachePermissionRule.Remove(t.Key));
            }

            foreach (var i in result.Content)
            {
                var id = i.Id;

                if (cachePermissionRule.TryAdd(id, i)) continue;

                cachePermissionRule[id] = i;
            }

            _cache.Set(ConfigSettings.CACHE_PERMISSION_RULE, cachePermissionRule);
        }
        finally
        {
            Locker.Release(nameof(UpdatePermissionAsync));
        }
    }

    public void Dispose()
    {
        _subscriber?.Dispose();
    }
}

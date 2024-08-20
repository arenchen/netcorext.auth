using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Services.Permission.Queries;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Contracts;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Extensions.Threading;
using Netcorext.Mediator;
using Netcorext.Serialization;
using Netcorext.Worker;

namespace Netcorext.Auth.Authentication.Workers;

internal class RoleRunner : IWorkerRunner<AuthWorker>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RedisClient _redis;
    private readonly IMemoryCache _cache;
    private readonly ISerializer _serializer;
    private readonly KeyLocker _locker;
    private readonly ConfigSettings _config;
    private readonly ILogger<RoleRunner> _logger;
    private IDisposable? _subscriber;

    public RoleRunner(IServiceProvider serviceProvider, RedisClient redis, IMemoryCache cache, ISerializer serializer, KeyLocker locker, IOptions<ConfigSettings> config, ILogger<RoleRunner> logger)
    {
        _serviceProvider = serviceProvider;
        _redis = redis;
        _cache = cache;
        _serializer = serializer;
        _locker = locker;
        _config = config.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(AuthWorker worker, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{Message}", nameof(RoleRunner));

        _subscriber?.Dispose();

        _subscriber = _redis.Subscribe(_config.Queues[ConfigSettings.QUEUES_ROLE_CHANGE_EVENT], Handler);

        await UpdateRoleAsync(null, cancellationToken);

        return;

        async void Handler(string s, object o)
        {
            await UpdateRoleAsync(o.ToString(), cancellationToken);
        }
    }

    private async Task UpdateRoleAsync(string? ids, CancellationToken cancellationToken = default)
    {
        try
        {
            await _locker.WaitAsync(nameof(UpdateRoleAsync));

            _logger.LogInformation(nameof(UpdateRoleAsync));

            using var scope = _serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            var reqIds = ids == null ? null : _serializer.Deserialize<long[]>(ids);

            var cacheRolePermission = _cache.Get<Dictionary<string, Services.Permission.Queries.Models.RolePermission>>(ConfigSettings.CACHE_ROLE_PERMISSION) ?? new Dictionary<string, Services.Permission.Queries.Models.RolePermission>();
            var cacheRolePermissionCondition = _cache.Get<Dictionary<long, Services.Permission.Queries.Models.RolePermissionCondition>>(ConfigSettings.CACHE_ROLE_PERMISSION_CONDITION) ?? new Dictionary<long, Services.Permission.Queries.Models.RolePermissionCondition>();

            if (reqIds != null && reqIds.Any())
            {
                var permissions = cacheRolePermission.Where(t => reqIds.Contains(t.Value.RoleId))
                                                     .ToArray();

                permissions.ForEach(t => cacheRolePermission.Remove(t.Key));


                var conditions = cacheRolePermissionCondition.Where(t => reqIds.Contains(t.Value.RoleId))
                                                             .ToArray();

                conditions.ForEach(t => cacheRolePermissionCondition.Remove(t.Key));
            }

            var result = await dispatcher.SendAsync(new GetRolePermission
                                                    {
                                                        Ids = reqIds
                                                    }, cancellationToken);

            if (result.Code == Result.Success && !result.Content.IsEmpty())
            {
                foreach (var i in result.Content)
                {
                    var id = $"{i.Id}-${i.PermissionId}";

                    if (cacheRolePermission.TryAdd(id, i)) continue;

                    cacheRolePermission[id] = i;
                }

                _cache.Set(ConfigSettings.CACHE_ROLE_PERMISSION, cacheRolePermission);
            }

            var resultCondition = await dispatcher.SendAsync(new GetRolePermissionCondition
                                                             {
                                                                 Ids = reqIds
                                                             }, cancellationToken);

            if (resultCondition.Code == Result.Success && !resultCondition.Content.IsEmpty())
            {
                foreach (var i in resultCondition.Content)
                {
                    var id = i.Id;

                    if (cacheRolePermissionCondition.TryAdd(id, i)) continue;

                    cacheRolePermissionCondition[id] = i;
                }

                _cache.Set(ConfigSettings.CACHE_ROLE_PERMISSION_CONDITION, cacheRolePermissionCondition);
            }
        }
        finally
        {
            _locker.Release(nameof(UpdateRoleAsync));
        }
    }

    public void Dispose()
    {
        _subscriber?.Dispose();
    }
}

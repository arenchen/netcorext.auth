using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Services.Permission.Queries;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Contracts;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;
using Netcorext.Serialization;
using Netcorext.Worker;

namespace Netcorext.Auth.Authentication.Workers;

internal class RoleRunner : IWorkerRunner<AuthWorker>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RedisClient _redis;
    private IDisposable? _subscriber;
    private readonly IMemoryCache _cache;
    private readonly ISerializer _serializer;
    private readonly ConfigSettings _config;
    private readonly ILogger<RoleRunner> _logger;
    private static readonly SemaphoreSlim RoleUpdateLocker = new(1, 1);

    public RoleRunner(IServiceProvider serviceProvider, RedisClient redis, IMemoryCache cache, ISerializer serializer, IOptions<ConfigSettings> config, ILogger<RoleRunner> logger)
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

        _subscriber = _redis.Subscribe(_config.Queues[ConfigSettings.QUEUES_ROLE_CHANGE_EVENT], (s, o) => UpdateRoleAsync(o.ToString(), cancellationToken).GetAwaiter().GetResult());

        await UpdateRoleAsync(null, cancellationToken);
    }

    private async Task UpdateRoleAsync(string? ids, CancellationToken cancellationToken = default)
    {
        try
        {
            await RoleUpdateLocker.WaitAsync(cancellationToken);

            _logger.LogInformation(nameof(UpdateRoleAsync));

            using var scope = _serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            var reqIds = ids == null ? null : _serializer.Deserialize<long[]>(ids);

            var result = await dispatcher.SendAsync(new GetRolePermission
                                                    {
                                                        Ids = reqIds
                                                    }, cancellationToken);

            if (result.Content == null || result.Code != Result.Success) return;

            var cacheRolePermissionRule = _cache.Get<Dictionary<string, Services.Permission.Queries.Models.RolePermissionRule>>(ConfigSettings.CACHE_ROLE_PERMISSION_RULE) ?? new Dictionary<string, Services.Permission.Queries.Models.RolePermissionRule>();
            var cacheRolePermissionCondition = _cache.Get<Dictionary<long, Services.Permission.Queries.Models.RolePermissionCondition>>(ConfigSettings.CACHE_ROLE_PERMISSION_CONDITION) ?? new Dictionary<long, Services.Permission.Queries.Models.RolePermissionCondition>();

            if (reqIds != null && reqIds.Any())
            {
                var rules = cacheRolePermissionRule.Where(t => reqIds.Contains(t.Value.RoleId))
                                                   .ToArray();

                rules.ForEach(t => cacheRolePermissionRule.Remove(t.Key));
                
                var conditions = cacheRolePermissionCondition.Where(t => reqIds.Contains(t.Value.RoleId))
                                                             .ToArray();

                conditions.ForEach(t => cacheRolePermissionCondition.Remove(t.Key));
            }

            foreach (var i in result.Content.PermissionRules)
            {
                var id = $"{i.RoleId}-{i.PermissionId}-{i.Id}";

                if (cacheRolePermissionRule.TryAdd(id, i)) continue;

                cacheRolePermissionRule[id] = i;
            }

            _cache.Set(ConfigSettings.CACHE_ROLE_PERMISSION_RULE, cacheRolePermissionRule);

            foreach (var i in result.Content.PermissionConditions)
            {
                if (cacheRolePermissionCondition.TryAdd(i.Id, i)) continue;

                cacheRolePermissionCondition[i.Id] = i;
            }

            _cache.Set(ConfigSettings.CACHE_ROLE_PERMISSION_CONDITION, cacheRolePermissionCondition);
        }
        finally
        {
            RoleUpdateLocker.Release();
        }
    }

    public void Dispose()
    {
        _subscriber?.Dispose();
    }
}
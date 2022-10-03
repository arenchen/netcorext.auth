using System.Text.Json;
using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Services.Permission.Queries;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Contracts;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;
using Netcorext.Worker;

namespace Netcorext.Auth.Authentication.Workers;

internal class RoleRunner : IWorkerRunner<AuthWorker>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RedisClient _redis;
    private readonly IMemoryCache _cache;
    private readonly ConfigSettings _config;
    private readonly ILogger<RoleRunner> _logger;
    private IDisposable? _subscription;
    private static readonly SemaphoreSlim RoleUpdateLocker = new(1, 1);

    public RoleRunner(IServiceProvider serviceProvider, RedisClient redis, IMemoryCache cache, IOptions<ConfigSettings> config, ILogger<RoleRunner> logger)
    {
        _serviceProvider = serviceProvider;
        _redis = redis;
        _cache = cache;
        _config = config.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(AuthWorker worker, CancellationToken cancellationToken = default)
    {
        _subscription?.Dispose();

        _subscription = _redis.Subscribe(_config.Queues[ConfigSettings.QUEUES_ROLE_CHANGE_EVENT], (s, o) => UpdateRoleAsync(o.ToString(), cancellationToken).GetAwaiter().GetResult());

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

            var reqIds = ids == null ? null : JsonSerializer.Deserialize<long[]>(ids);

            var result = await dispatcher.SendAsync(new GetRolePermission
                                                    {
                                                        Ids = reqIds
                                                    }, cancellationToken);

            if (result?.Content == null || result.Code != Result.Success) return;

            var cacheRolePermissionRule = _cache.Get<Dictionary<string, Services.Permission.Queries.Models.RolePermissionRule>>(ConfigSettings.CACHE_ROLE_PERMISSION_RULE) ?? new Dictionary<string, Services.Permission.Queries.Models.RolePermissionRule>();
            var cacheRolePermissionCondition = _cache.Get<Dictionary<long, Services.Permission.Queries.Models.RolePermissionCondition>>(ConfigSettings.CACHE_ROLE_PERMISSION_CONDITION) ?? new Dictionary<long, Services.Permission.Queries.Models.RolePermissionCondition>();

            if (reqIds != null && reqIds.Any())
            {
                var repIds = result.Content.PermissionRules.Select(t => t.RoleId);

                var diffIds = reqIds.Except(repIds);

                var rules = cacheRolePermissionRule.Where(t => diffIds.Contains(t.Value.RoleId))
                                                   .ToArray();

                rules.ForEach(t => cacheRolePermissionRule.Remove(t.Key));

                repIds = result.Content.PermissionConditions.Select(t => t.RoleId);

                diffIds = reqIds.Except(repIds);

                var conditions = cacheRolePermissionCondition.Where(t => diffIds.Contains(t.Value.RoleId))
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
        _subscription?.Dispose();
    }
}
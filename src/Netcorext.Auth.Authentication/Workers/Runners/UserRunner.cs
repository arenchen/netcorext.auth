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

internal class UserRunner : IWorkerRunner<AuthWorker>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RedisClient _redis;
    private IDisposable? _subscriber;
    private readonly IMemoryCache _cache;
    private readonly ISerializer _serializer;
    private readonly ConfigSettings _config;
    private readonly ILogger<UserRunner> _logger;
    private static readonly SemaphoreSlim UserUpdateLocker = new(1, 1);

    public UserRunner(IServiceProvider serviceProvider, RedisClient redis, IMemoryCache cache, ISerializer serializer, IOptions<ConfigSettings> config, ILogger<UserRunner> logger)
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
        _logger.LogDebug("{Message}", nameof(UserRunner));

        _subscriber?.Dispose();

        _subscriber = _redis.Subscribe(new[]
                                       {
                                           _config.Queues[ConfigSettings.QUEUES_USER_CHANGE_EVENT]
                                       },
                                       async (s, o) => await UpdateUserAsync(o.ToString(), cancellationToken));

        await UpdateUserAsync(null, cancellationToken);
    }

    private async Task UpdateUserAsync(string? ids, CancellationToken cancellationToken = default)
    {
        try
        {
            await UserUpdateLocker.WaitAsync(cancellationToken);

            _logger.LogInformation(nameof(UpdateUserAsync));

            using var scope = _serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            var reqIds = ids == null ? null : _serializer.Deserialize<long[]>(ids);

            var result = await dispatcher.SendAsync(new GetUserPermission
                                                    {
                                                        Ids = reqIds
                                                    }, cancellationToken);

            if (result.Content == null || result.Code != Result.Success) return;

            var cacheUserPermissionCondition = _cache.Get<Dictionary<long, Services.Permission.Queries.Models.UserPermissionCondition>>(ConfigSettings.CACHE_USER_PERMISSION_CONDITION) ?? new Dictionary<long, Services.Permission.Queries.Models.UserPermissionCondition>();

            if (reqIds != null && reqIds.Any())
            {
                var repIds = result.Content.PermissionConditions.Select(t => t.UserId);

                var diffIds = reqIds.Except(repIds);

                var conditions = cacheUserPermissionCondition.Where(t => diffIds.Contains(t.Value.UserId))
                                                             .ToArray();

                conditions.ForEach(t => cacheUserPermissionCondition.Remove(t.Key));
            }

            foreach (var i in result.Content.PermissionConditions)
            {
                if (cacheUserPermissionCondition.TryAdd(i.Id, i)) continue;

                cacheUserPermissionCondition[i.Id] = i;
            }

            _cache.Set(ConfigSettings.CACHE_USER_PERMISSION_CONDITION, cacheUserPermissionCondition);
        }
        finally
        {
            UserUpdateLocker.Release();
        }
    }

    public void Dispose()
    {
        _subscriber?.Dispose();
    }
}
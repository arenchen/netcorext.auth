using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Services.Permission.Queries;
using Netcorext.Auth.Authentication.Services.Token.Commands;
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
    private static readonly SemaphoreSlim TokenUpdateLocker = new(1, 1);

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

                                           // 目前在 Netcorext.Auth.API 異動時處理
                                           // _config.Queues[ConfigSettings.QUEUES_USER_ROLE_CHANGE_EVENT]
                                       }, Handler);

        await UpdateUserAsync(null, cancellationToken);

        return;

        async void Handler(string s, object o)
        {
            if (s == _config.Queues[ConfigSettings.QUEUES_USER_CHANGE_EVENT])
                await UpdateUserAsync(o.ToString(), cancellationToken);
            else if (s == _config.Queues[ConfigSettings.QUEUES_USER_ROLE_CHANGE_EVENT])
                await BlockUserTokenAsync(o.ToString(), cancellationToken);
        }
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

            var result = await dispatcher.SendAsync(new GetUserPermissionCondition
                                                    {
                                                        Ids = reqIds
                                                    }, cancellationToken);

            if (result.Content == null || result.Code != Result.Success) return;

            var cacheUserPermissionCondition = _cache.Get<Dictionary<long, Services.Permission.Queries.Models.UserPermissionCondition>>(ConfigSettings.CACHE_USER_PERMISSION_CONDITION) ?? new Dictionary<long, Services.Permission.Queries.Models.UserPermissionCondition>();

            if (reqIds != null && reqIds.Any())
            {
                var conditions = cacheUserPermissionCondition.Where(t => reqIds.Contains(t.Value.UserId))
                                                             .ToArray();

                conditions.ForEach(t => cacheUserPermissionCondition.Remove(t.Key));
            }

            foreach (var i in result.Content)
            {
                var id = i.Id;

                if (cacheUserPermissionCondition.TryAdd(id, i)) continue;

                cacheUserPermissionCondition[id] = i;
            }

            _cache.Set(ConfigSettings.CACHE_USER_PERMISSION_CONDITION, cacheUserPermissionCondition);
        }
        finally
        {
            UserUpdateLocker.Release();
        }
    }

    private async Task BlockUserTokenAsync(string? ids, CancellationToken cancellationToken = default)
    {
        if (ids == null)
            return;

        try
        {
            await TokenUpdateLocker.WaitAsync(cancellationToken);

            _logger.LogInformation(nameof(BlockUserTokenAsync));

            using var scope = _serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            var reqIds = _serializer.Deserialize<long[]>(ids);

            if (reqIds == null || !reqIds.Any())
                return;

            await dispatcher.SendAsync(new BlockUserToken
                                       {
                                           Ids = reqIds
                                       }, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
        }
        finally
        {
            TokenUpdateLocker.Release();
        }
    }

    public void Dispose()
    {
        _subscriber?.Dispose();
    }
}

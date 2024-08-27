using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Services.Permission.Queries;
using Netcorext.Auth.Authentication.Services.User.Queries;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Contracts;
using Netcorext.Extensions.Linq;
using Netcorext.Extensions.Threading;
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
    private readonly MemoryCacheEntryOptions _cacheEntryOptions;
    private readonly ISerializer _serializer;
    private readonly KeyLocker _locker;
    private readonly ConfigSettings _config;
    private readonly ILogger<UserRunner> _logger;

    public UserRunner(IServiceProvider serviceProvider, RedisClient redis, IMemoryCache cache, MemoryCacheEntryOptions cacheEntryOptions, ISerializer serializer, KeyLocker locker, IOptions<ConfigSettings> config, ILogger<UserRunner> logger)
    {
        _serviceProvider = serviceProvider;
        _redis = redis;
        _cache = cache;
        _cacheEntryOptions = cacheEntryOptions;
        _serializer = serializer;
        _locker = locker;
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
                                       }, Handler);

        await UpdateUserAsync(null, cancellationToken);
        await BlockUserAsync(null, cancellationToken);

        return;

        async void Handler(string s, object o)
        {
            await UpdateUserAsync(o.ToString(), cancellationToken);
            await BlockUserAsync(o.ToString(), cancellationToken);
        }
    }

    private async Task UpdateUserAsync(string? ids, CancellationToken cancellationToken = default)
    {
        try
        {
            await _locker.WaitAsync(nameof(UpdateUserAsync));

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

            _cache.Set(ConfigSettings.CACHE_USER_PERMISSION_CONDITION, cacheUserPermissionCondition, _cacheEntryOptions);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
        }
        finally
        {
            _locker.Release(nameof(UpdateUserAsync));
        }
    }

    private async Task BlockUserAsync(string? ids, CancellationToken cancellationToken = default)
    {
        try
        {
            await _locker.WaitAsync(nameof(BlockUserAsync));

            _logger.LogInformation(nameof(BlockUserAsync));

            using var scope = _serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            var reqIds = ids == null ? null : _serializer.Deserialize<long[]>(ids);

            var result = await dispatcher.SendAsync(new GetBlockedUser
                                                    {
                                                        Ids = reqIds
                                                    }, cancellationToken);

            var cacheBlockedUser = _cache.Get<HashSet<long>>(ConfigSettings.CACHE_BLOCKED_USER) ?? new HashSet<long>();

            if (reqIds == null || !reqIds.Any())
            {
                cacheBlockedUser.Clear();

                if (result.Content != null && result.Content.Any())
                    result.Content.ForEach(t => cacheBlockedUser.Add(t));
            }
            else if (result.Content == null || !result.Content.Any())
            {
                cacheBlockedUser.RemoveWhere(t => reqIds.Contains(t));
            }
            else
            {
                result.Content.ForEach(t => cacheBlockedUser.Add(t));
            }

            _cache.Set(ConfigSettings.CACHE_BLOCKED_USER, cacheBlockedUser, _cacheEntryOptions);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
        }
        finally
        {
            _locker.Release(nameof(BlockUserAsync));
        }
    }

    public void Dispose()
    {
        _subscriber?.Dispose();
    }
}

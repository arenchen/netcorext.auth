using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Services.Client.Queries;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Extensions.Linq;
using Netcorext.Extensions.Threading;
using Netcorext.Mediator;
using Netcorext.Serialization;
using Netcorext.Worker;

namespace Netcorext.Auth.Authentication.Workers;

internal class ClientRunner : IWorkerRunner<AuthWorker>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RedisClient _redis;
    private IDisposable? _subscriber;
    private readonly IMemoryCache _cache;
    private readonly ISerializer _serializer;
    private readonly KeyLocker _locker;
    private readonly ConfigSettings _config;
    private readonly ILogger<UserRunner> _logger;

    public ClientRunner(IServiceProvider serviceProvider, RedisClient redis, IMemoryCache cache, ISerializer serializer, KeyLocker locker, IOptions<ConfigSettings> config, ILogger<UserRunner> logger)
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
        _logger.LogDebug("{Message}", nameof(ClientRunner));

        _subscriber?.Dispose();

        _subscriber = _redis.Subscribe(new[]
                                       {
                                           _config.Queues[ConfigSettings.QUEUES_CLIENT_CHANGE_EVENT]
                                       }, Handler);

        await UpdateClientAsync(null, cancellationToken);
        await BlockClientAsync(null, cancellationToken);

        return;

        async void Handler(string s, object o)
        {
            await UpdateClientAsync(o.ToString(), cancellationToken);
            await BlockClientAsync(o.ToString(), cancellationToken);
        }
    }

    private async Task UpdateClientAsync(string? ids, CancellationToken cancellationToken = default)
    {
        try
        {
            await _locker.WaitAsync(nameof(UpdateClientAsync));

            _logger.LogInformation(nameof(UpdateClientAsync));

            using var scope = _serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            var reqIds = ids == null ? null : _serializer.Deserialize<long[]>(ids);

            var result = await dispatcher.SendAsync(new GetClient
                                                    {
                                                        Ids = reqIds
                                                    }, cancellationToken);

            var cacheClient = _cache.Get<Dictionary<long, Netcorext.Auth.Authentication.Services.Client.Queries.Models.Client>>(ConfigSettings.CACHE_CLIENT) ?? new Dictionary<long, Netcorext.Auth.Authentication.Services.Client.Queries.Models.Client>();

            if (reqIds == null || !reqIds.Any())
            {
                cacheClient.Clear();

                if (result.Content != null && result.Content.Any())
                    result.Content.ForEach(t => cacheClient.TryAdd(t.Id, t));
            }
            else if (result.Content == null || !result.Content.Any())
            {
                reqIds.ForEach(t => cacheClient.Remove(t));
            }
            else
            {
                var diffIds = reqIds.Except(result.Content.Select(t => t.Id)).ToArray();
                diffIds.ForEach(t => cacheClient.Remove(t));
                result.Content.ForEach(t => cacheClient.Add(t.Id, t));
            }

            _cache.Set(ConfigSettings.CACHE_CLIENT, cacheClient);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
        }
        finally
        {
            _locker.Release(nameof(BlockClientAsync));
        }
    }

    private async Task BlockClientAsync(string? ids, CancellationToken cancellationToken = default)
    {
        try
        {
            await _locker.WaitAsync(nameof(BlockClientAsync));

            _logger.LogInformation(nameof(BlockClientAsync));

            using var scope = _serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            var reqIds = ids == null ? null : _serializer.Deserialize<long[]>(ids);

            var result = await dispatcher.SendAsync(new GetBlockedClient
                                       {
                                           Ids = reqIds
                                       }, cancellationToken);

            var cacheBlockedClient = _cache.Get<HashSet<long>>(ConfigSettings.CACHE_BLOCKED_CLIENT) ?? new HashSet<long>();

            if (reqIds == null || !reqIds.Any())
            {
                cacheBlockedClient.Clear();

                if (result.Content != null && result.Content.Any())
                    result.Content.ForEach(t => cacheBlockedClient.Add(t));
            }
            else if (result.Content == null || !result.Content.Any())
            {
                cacheBlockedClient.RemoveWhere(t => reqIds.Contains(t));
            }
            else
            {
                result.Content.ForEach(t => cacheBlockedClient.Add(t));
            }

            _cache.Set(ConfigSettings.CACHE_BLOCKED_CLIENT, cacheBlockedClient);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
        }
        finally
        {
            _locker.Release(nameof(BlockClientAsync));
        }
    }

    public void Dispose()
    {
        _subscriber?.Dispose();
    }
}

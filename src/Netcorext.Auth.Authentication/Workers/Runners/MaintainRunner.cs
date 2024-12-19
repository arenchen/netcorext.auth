using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Services.Maintenance.Queries;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Contracts;
using Netcorext.Mediator;
using Netcorext.Serialization;
using Netcorext.Worker;

namespace Netcorext.Auth.Authentication.Workers;

internal class MaintainRunner : IWorkerRunner<AuthWorker>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RedisClient _redis;
    private IDisposable? _subscriber;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheEntryOptions;
    private readonly ISerializer _serializer;
    private readonly KeyLocker _locker;
    private readonly ConfigSettings _config;
    private readonly ILogger<MaintainRunner> _logger;

    public MaintainRunner(IServiceProvider serviceProvider, RedisClient redis, IMemoryCache cache, MemoryCacheEntryOptions cacheEntryOptions, ISerializer serializer, KeyLocker locker, IOptions<ConfigSettings> config, ILogger<MaintainRunner> logger)
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
        _logger.LogDebug("{Message}", nameof(MaintainRunner));

        _subscriber?.Dispose();

        _subscriber = _redis.Subscribe(_config.Queues[ConfigSettings.QUEUES_MAINTAIN_CHANGE_EVENT], Handler);

        await UpdateMaintainAsync(null, cancellationToken);

        return;

        async void Handler(string s, object o)
        {
            await UpdateMaintainAsync(o.ToString(), cancellationToken);
        }
    }

    private async Task UpdateMaintainAsync(string? data, CancellationToken cancellationToken = default)
    {
        try
        {
            await _locker.WaitAsync(nameof(UpdateMaintainAsync));

            _logger.LogInformation(nameof(UpdateMaintainAsync));

            using var scope = _serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();
            var result = await dispatcher.SendAsync(new GetMaintain(), cancellationToken);

            if (result.Content == null || result.Code != Result.Success) return;

            _cache.Set($"{ConfigSettings.CACHE_MAINTAIN}", result.Content, _cacheEntryOptions);
            _cache.Set(ConfigSettings.CACHE_MAINTAIN_CHECK_KEY, 1);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
        }
        finally
        {
            _locker.Release(nameof(UpdateMaintainAsync));
        }
    }

    public void Dispose()
    {
        _subscriber?.Dispose();
    }
}

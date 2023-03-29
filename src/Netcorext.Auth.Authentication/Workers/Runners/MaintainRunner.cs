using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Services.Maintenance.Commands;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Worker;

namespace Netcorext.Auth.Authentication.Workers;

internal class MaintainRunner : IWorkerRunner<AuthWorker>
{
    private readonly RedisClient _redis;
    private IDisposable? _subscriber;
    private readonly IMemoryCache _cache;
    private readonly ConfigSettings _config;
    private readonly ILogger<MaintainRunner> _logger;
    private static readonly SemaphoreSlim MaintainUpdateLocker = new(1, 1);

    public MaintainRunner(RedisClient redis, IMemoryCache cache, IOptions<ConfigSettings> config, ILogger<MaintainRunner> logger)
    {
        _redis = redis;
        _cache = cache;
        _config = config.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(AuthWorker worker, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{Message}", nameof(MaintainRunner));

        _subscriber?.Dispose();

        _subscriber = _redis.Subscribe(_config.Queues[ConfigSettings.QUEUES_MAINTAIN_CHANGE_EVENT], async (s, o) => await UpdateMaintainAsync(o.ToString(), cancellationToken));

        await UpdateMaintainAsync(null, cancellationToken);
    }

    private async Task UpdateMaintainAsync(string? data, CancellationToken cancellationToken = default)
    {
        try
        {
            await MaintainUpdateLocker.WaitAsync(cancellationToken);

            _logger.LogInformation(nameof(UpdateMaintainAsync));

            if (data == bool.FalseString)
            {
                _cache.Remove(ConfigSettings.CACHE_MAINTAIN);

                return;
            }

            var cacheConfig = _config.Caches[ConfigSettings.CACHE_MAINTAIN];
            var cacheMaintain = await _redis.GetAsync<Maintain>(cacheConfig.Key);

            _cache.Set(ConfigSettings.CACHE_MAINTAIN, cacheMaintain);
        }
        finally
        {
            MaintainUpdateLocker.Release();
        }
    }

    public void Dispose()
    {
        _subscriber?.Dispose();
    }
}
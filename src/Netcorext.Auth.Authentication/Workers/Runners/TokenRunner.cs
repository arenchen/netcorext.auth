using System.Text.Json;
using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Worker;

namespace Netcorext.Auth.Authentication.Workers;

internal class TokenRunner : IWorkerRunner<AuthWorker>
{
    private readonly RedisClient _redis;
    private readonly IMemoryCache _cache;
    private readonly ConfigSettings _config;
    private readonly ILogger<TokenRunner> _logger;
    private IDisposable? _subscription;
    private static readonly SemaphoreSlim TokenUpdateLocker = new(1, 1);

    public TokenRunner(RedisClient redis, IMemoryCache cache, IOptions<ConfigSettings> config, ILogger<TokenRunner> logger)
    {
        _redis = redis;
        _cache = cache;
        _config = config.Value;
        _logger = logger;
    }

    public Task InvokeAsync(AuthWorker worker, CancellationToken cancellationToken = default)
    {
        _subscription?.Dispose();

        _subscription = _redis.Subscribe(_config.Queues[ConfigSettings.QUEUES_TOKEN_REVOKE_EVENT], (s, o) => UpdateTokenAsync(o.ToString(), cancellationToken).GetAwaiter().GetResult());

        return Task.CompletedTask;
    }

    private async Task UpdateTokenAsync(string? data, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(data)) return;

        try
        {
            await TokenUpdateLocker.WaitAsync(cancellationToken);

            _logger.LogInformation(nameof(UpdateTokenAsync));

            var cachePermissions = _cache.Get<Dictionary<string, bool>>(ConfigSettings.CACHE_TOKEN) ?? new Dictionary<string, bool>();

            var tokens = JsonSerializer.Deserialize<string[]>(data);

            if (tokens == null) return;

            foreach (var token in tokens)
            {
                if (cachePermissions.TryAdd(token, false)) continue;

                cachePermissions[token] = false;
            }

            _cache.Set(ConfigSettings.CACHE_TOKEN, cachePermissions, DateTimeOffset.UtcNow.AddMilliseconds(_config.AppSettings.CacheTokenExpires));
        }
        finally
        {
            TokenUpdateLocker.Release();
        }
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Serialization;
using Netcorext.Worker;

namespace Netcorext.Auth.Authentication.Workers;

internal class TokenRunner : IWorkerRunner<AuthWorker>
{
    private readonly RedisClient _redis;
    private IDisposable? _subscriber;
    private readonly IMemoryCache _cache;
    private readonly ISerializer _serializer;
    private readonly ConfigSettings _config;
    private readonly ILogger<TokenRunner> _logger;
    private static readonly SemaphoreSlim TokenUpdateLocker = new(1, 1);

    public TokenRunner(RedisClient redis, IMemoryCache cache, ISerializer serializer, IOptions<ConfigSettings> config, ILogger<TokenRunner> logger)
    {
        _redis = redis;
        _cache = cache;
        _serializer = serializer;
        _config = config.Value;
        _logger = logger;
    }

    public Task InvokeAsync(AuthWorker worker, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{Message}", nameof(TokenRunner));

        _subscriber?.Dispose();

        _subscriber = _redis.Subscribe(_config.Queues[ConfigSettings.QUEUES_TOKEN_REVOKE_EVENT], async (s, o) => await UpdateTokenAsync(o.ToString(), cancellationToken));

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

            var tokens = _serializer.Deserialize<string[]>(data);

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
        _subscriber?.Dispose();
    }
}
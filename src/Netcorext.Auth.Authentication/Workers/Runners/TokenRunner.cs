using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Contracts;
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

        _subscriber = _redis.Subscribe(_config.Queues[ConfigSettings.QUEUES_TOKEN_REVOKE_EVENT], Handler);

        return Task.CompletedTask;

        async void Handler(string s, object o)
        {
            await UpdateTokenAsync(o.ToString(), cancellationToken);
        }
    }

    private async Task UpdateTokenAsync(string? data, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(data)) return;

        try
        {
            await TokenUpdateLocker.WaitAsync(cancellationToken);

            _logger.LogInformation(nameof(UpdateTokenAsync));

            var tokens = _serializer.Deserialize<string[]>(data);

            if (tokens == null || !tokens.Any()) return;

            foreach (var token in tokens)
            {
                _cache.Set(token, Result.UnauthorizedAndCannotRefreshToken, DateTimeOffset.UtcNow.AddMilliseconds(_config.AppSettings.CacheTokenExpires));
            }
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

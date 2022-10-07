using FreeRedis;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Worker;

namespace Netcorext.Auth.Authentication.Workers;

internal class HealthCheckRunner : IWorkerRunner<AuthWorker>
{
    private AuthWorker _worker = null!;
    private readonly RedisClient _redis;
    private readonly ConfigSettings _config;
    private readonly ILogger<HealthCheckRunner> _logger;
    private IDisposable? _subscription;
    private static DateTimeOffset _lastHealthCheckTime = DateTimeOffset.UtcNow;
    private static readonly SemaphoreSlim HealthCheckLocker = new(1, 1);

    public HealthCheckRunner(RedisClient redis, IOptions<ConfigSettings> config, ILogger<HealthCheckRunner> logger)
    {
        _redis = redis;
        _config = config.Value;
        _logger = logger;
    }

    public Task InvokeAsync(AuthWorker worker, CancellationToken cancellationToken = default)
    {
        _worker = worker;

        _lastHealthCheckTime = DateTimeOffset.UtcNow;

        _subscription?.Dispose();

        _subscription = _redis.Subscribe(_config.Queues[ConfigSettings.QUEUES_HEALTH_CHECK_EVENT], (s, o) => HealthCheckHandleAsync(cancellationToken).GetAwaiter().GetResult());

        return HealthCheckAsync(cancellationToken);
    }

    private Task HealthCheckHandleAsync(CancellationToken cancellationToken = default)
    {
        _lastHealthCheckTime = DateTimeOffset.UtcNow;

        _logger.LogInformation("PONG");

        return Task.CompletedTask;
    }

    private async Task HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await HealthCheckLocker.WaitAsync(cancellationToken);

                if (_lastHealthCheckTime.AddMilliseconds(_config.AppSettings.HealthCheckInterval) < DateTimeOffset.UtcNow)
                {
                    // _logger.LogInformation("PING");

                    _redis.Publish(_config.Queues[ConfigSettings.QUEUES_HEALTH_CHECK_EVENT], "PING");
                }
                else if (_lastHealthCheckTime.AddMilliseconds(_config.AppSettings.HealthCheckTimeout) < DateTimeOffset.UtcNow)
                {
                    await _worker.StopAsync(cancellationToken);
                    await _worker.StartAsync(cancellationToken);
                }
            }
            finally
            {
                HealthCheckLocker.Release();

                await Task.Delay(1000);
            }
        }
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
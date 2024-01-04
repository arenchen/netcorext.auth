using Microsoft.Extensions.Options;
using Netcorext.Auth.Gateway.Settings;
using Netcorext.Worker;

namespace Netcorext.Auth.Gateway.Workers;

internal class AuthWorker : BackgroundWorker
{
    private readonly ConfigSettings _config;
    private readonly IEnumerable<IWorkerRunner<AuthWorker>> _runners;
    private readonly ILogger<AuthWorker> _logger;
    private int _retryCount;

    public AuthWorker(IOptions<ConfigSettings> config, IEnumerable<IWorkerRunner<AuthWorker>> runners, ILogger<AuthWorker> logger)
    {
        _config = config.Value;
        _runners = runners;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var ct = new CancellationTokenSource();

        try
        {
            await Task.WhenAll(_runners.Select(t => t.InvokeAsync(this, ct.Token)));

            _retryCount = 0;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "${Message}", e.Message);

            ct.Cancel();

            _retryCount++;

            if (_retryCount <= _config.AppSettings.RetryLimit)
                await ExecuteAsync(cancellationToken);
        }
    }

    public override void Dispose()
    {
        foreach (var disposable in _runners)
        {
            disposable.Dispose();
        }

        base.Dispose();
    }
}

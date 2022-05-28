using Netcorext.Worker;

namespace Netcorext.Auth.Authentication.Workers;

internal class AuthWorker : BackgroundWorker
{
    private readonly IEnumerable<IWorkerRunner<AuthWorker>> _runners;

    public AuthWorker(IEnumerable<IWorkerRunner<AuthWorker>> runners)
    {
        _runners = runners;
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(_runners.Select(t => t.InvokeAsync(this, cancellationToken)));
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
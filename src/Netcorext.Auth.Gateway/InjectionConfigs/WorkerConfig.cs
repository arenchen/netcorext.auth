using Netcorext.Auth.Gateway.Workers;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

[Injection]
public class WorkerConfig
{
    public WorkerConfig(IServiceCollection services)
    {
        services.AddWorkerRunner<AuthWorker, RouteRunner>();
    }
}

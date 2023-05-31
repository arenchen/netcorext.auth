using Netcorext.Auth.Authentication.Workers;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

[Injection]
public class WorkerConfig
{
    public WorkerConfig(IServiceCollection services)
    {
        services.AddWorkerRunner<AuthWorker, TokenRunner>();
        services.AddWorkerRunner<AuthWorker, RoleRunner>();
        services.AddWorkerRunner<AuthWorker, UserRunner>();
        services.AddWorkerRunner<AuthWorker, RouteRunner>();
        services.AddWorkerRunner<AuthWorker, MaintainRunner>();
    }
}
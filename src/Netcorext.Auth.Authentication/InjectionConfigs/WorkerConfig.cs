using Netcorext.Auth.Authentication.Workers;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

[Injection]
public class WorkerConfig
{
    public WorkerConfig(IServiceCollection services)
    {
        services.AddWorkerRunner<AuthWorker, BlockedIpRunner>();
        services.AddWorkerRunner<AuthWorker, MaintainRunner>();
        services.AddWorkerRunner<AuthWorker, PermissionRunner>();
        services.AddWorkerRunner<AuthWorker, RoleRunner>();
        services.AddWorkerRunner<AuthWorker, RouteRunner>();
        services.AddWorkerRunner<AuthWorker, TokenRunner>();
        services.AddWorkerRunner<AuthWorker, UserRunner>();
    }
}

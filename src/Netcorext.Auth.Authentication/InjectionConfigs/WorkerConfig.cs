using Netcorext.Auth.Authentication.Workers;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

[Injection]
public class WorkerConfig
{
    public WorkerConfig(IServiceCollection services)
    {
        services.AddWorkerRunner<AuthWorker, RouteRunner>();
        services.AddWorkerRunner<AuthWorker, RoleRunner>();
        services.AddWorkerRunner<AuthWorker, PermissionRunner>();
        services.AddWorkerRunner<AuthWorker, UserRunner>();
        services.AddWorkerRunner<AuthWorker, ClientRunner>();
        services.AddWorkerRunner<AuthWorker, MaintainRunner>();
        services.AddWorkerRunner<AuthWorker, BlockedIpRunner>();
        services.AddWorkerRunner<AuthWorker, TokenRunner>();
        services.AddWorkerRunner<AuthWorker, TrafficRunner>();
    }
}

using Netcorext.Auth.Authentication.Workers;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

[Injection]
public class GatewayConfig
{
    public GatewayConfig(IServiceCollection services)
    {
        services.AddCors();

        services.AddReverseProxy()
                .LoadFromMemory();

        services.AddWorkerRunner<AuthWorker, TokenRunner>();
        services.AddWorkerRunner<AuthWorker, RoleRunner>();
        services.AddWorkerRunner<AuthWorker, UserRunner>();
        services.AddWorkerRunner<AuthWorker, RouteRunner>();
        services.AddWorkerRunner<AuthWorker, MaintainRunner>();
    }
}
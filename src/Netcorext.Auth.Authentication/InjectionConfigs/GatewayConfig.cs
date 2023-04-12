using Netcorext.Auth.Authentication.Workers;
using Yarp.ReverseProxy.Configuration;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

[Injection]
public class GatewayConfig
{
    public GatewayConfig(IServiceCollection services)
    {
        services.AddCors();

        services.AddReverseProxy()
                .LoadFromMemory(Array.Empty<RouteConfig>(), Array.Empty<ClusterConfig>());

        services.AddWorkerRunner<AuthWorker, TokenRunner>();
        services.AddWorkerRunner<AuthWorker, RoleRunner>();
        services.AddWorkerRunner<AuthWorker, UserRunner>();
        services.AddWorkerRunner<AuthWorker, RouteRunner>();
        services.AddWorkerRunner<AuthWorker, MaintainRunner>();
    }
}
using Netcorext.Auth.Authentication.Workers;
using Netcorext.Extensions.DependencyInjection;

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
        services.AddWorkerRunner<AuthWorker, RouteRunner>();
        services.AddWorkerRunner<AuthWorker, HealthCheckRunner>();
    }
}
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
    }
}
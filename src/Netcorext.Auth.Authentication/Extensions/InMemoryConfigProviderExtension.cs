using Yarp.ReverseProxy.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class InMemoryConfigProviderExtension
{
    public static IReverseProxyBuilder LoadFromMemory(this IReverseProxyBuilder builder) => LoadFromMemory(builder, Array.Empty<RouteConfig>(), Array.Empty<ClusterConfig>());

    public static IReverseProxyBuilder LoadFromMemory(this IReverseProxyBuilder builder, IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
    {
        builder.Services.AddSingleton<IProxyConfigProvider>(provider => new InMemoryConfigProvider(routes, clusters));

        return builder;
    }
}
using Yarp.ReverseProxy.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class InMemoryConfigProviderExtension
{
    public static IReverseProxyBuilder LoadFromMemory(this IReverseProxyBuilder builder) => builder.LoadFromMemory(Array.Empty<RouteConfig>(), Array.Empty<ClusterConfig>());
}
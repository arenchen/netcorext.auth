using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

[Injection]
public class GatewayConfig
{
    public GatewayConfig(IServiceCollection services, IConfiguration configuration)
    {
        var requestIdHeaderName = configuration.GetValue<string>("AppSettings:RequestIdHeaderName");

        services.AddCors();

        var gatewayConfig = configuration.GetSection("ReverseProxy");

        var proxyBuilder = services.AddReverseProxy();

        if (gatewayConfig.Exists())
            proxyBuilder.LoadFromConfig(gatewayConfig);
        else
            proxyBuilder.LoadFromMemory(Array.Empty<RouteConfig>(), Array.Empty<ClusterConfig>());

        proxyBuilder.AddTransforms(builder =>
                                   {
                                       builder.AddXForwarded(ForwardedTransformActions.Off);
                                       builder.AddXForwardedFor(action: ForwardedTransformActions.Append);

                                       builder.AddResponseTransform(ctx =>
                                                                    {
                                                                        if (ctx.ProxyResponse == null || !ctx.ProxyResponse.Headers.TryGetValues(requestIdHeaderName, out var requestIds))
                                                                            return ValueTask.CompletedTask;

                                                                        var requestIdHeader = string.Join(',', requestIds);

                                                                        if (string.IsNullOrWhiteSpace(requestIdHeader))
                                                                            return ValueTask.CompletedTask;

                                                                        ctx.HttpContext.Response.Headers[requestIdHeaderName] = requestIdHeader;

                                                                        return ValueTask.CompletedTask;
                                                                    });
                                   });
    }
}

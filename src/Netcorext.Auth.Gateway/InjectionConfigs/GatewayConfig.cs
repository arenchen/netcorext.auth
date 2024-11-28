using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace Netcorext.Auth.Gateway.InjectionConfigs;

[Injection]
public class GatewayConfig
{
    public GatewayConfig(IServiceCollection services, IConfiguration configuration)
    {
        var requestIdHeaderName = configuration.GetValue<string>("AppSettings:RequestIdHeaderName");

        services.AddCors();

        services.AddReverseProxy()
                .LoadFromMemory(Array.Empty<RouteConfig>(), Array.Empty<ClusterConfig>())
                .AddTransforms(builder =>
                               {
                                   builder.AddXForwarded(ForwardedTransformActions.Off);
                                   builder.AddXForwardedFor(action: ForwardedTransformActions.Append);
                                   builder.AddResponseTransform(ctx =>
                                                                {
                                                                    if (ctx.ProxyResponse == null || string.IsNullOrWhiteSpace(requestIdHeaderName) || !ctx.ProxyResponse.Headers.TryGetValues(requestIdHeaderName, out var requestIds))
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

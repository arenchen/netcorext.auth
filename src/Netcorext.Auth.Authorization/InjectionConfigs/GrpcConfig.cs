using Microsoft.Extensions.Options;
using Netcorext.Auth.Authorization.Settings;
using Netcorext.Auth.Protobufs;
using Netcorext.Extensions.DependencyInjection;
using Netcorext.Extensions.Grpc.Interceptors;

namespace Netcorext.Auth.Authorization.InjectionConfigs;

[Injection]
public class GrpcConfig
{
    public GrpcConfig(IServiceCollection services)
    {
        services.AddTransient<ExceptionInterceptor>();

        services.AddGrpc(options =>
                         {
                             options.Interceptors.Add<ExceptionInterceptor>();
                         });

        services.AddGrpcClient<RouteService.RouteServiceClient>((provider, options) =>
                                                                {
                                                                    var config = provider.GetRequiredService<IOptions<ConfigSettings>>().Value;
                                                                    var endpoint = new Uri(config.Services["Authentication"].Url);
                                                                    options.Address = endpoint;

                                                                    //options.InterceptorRegistrations.Add(new InterceptorRegistration(InterceptorScope.Client, creator => creator.GetRequiredService<ExceptionInterceptor>()));
                                                                    //options.ChannelOptionsActions.Add(opt => { opt.HttpHandler = new GrpcClientRouteHandler(endpoint.AbsolutePath); });
                                                                });
    }
}
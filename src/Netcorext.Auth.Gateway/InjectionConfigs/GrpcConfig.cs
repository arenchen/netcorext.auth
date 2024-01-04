using Netcorext.Extensions.Grpc.Interceptors;

namespace Netcorext.Auth.Gateway.InjectionConfigs;

[Injection]
public class GrpcConfig
{
    public GrpcConfig(IServiceCollection services)
    {
        services.AddGrpc(options => options.Interceptors.Add<ExceptionInterceptor>());
    }
}

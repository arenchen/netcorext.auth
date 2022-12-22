namespace Netcorext.Auth.Authentication.InjectionConfigs;

[Injection]
public class ServiceConfig
{
    public ServiceConfig(IServiceCollection services)
    {
        services.AddMediator()
                .AddRedisQueuing()
                .AddPerformancePipeline()
                .AddValidatorPipeline();
    }
}
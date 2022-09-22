namespace Netcorext.Auth.Authorization.InjectionConfigs;

[Injection]
public class ServiceConfig
{
    public ServiceConfig(IServiceCollection services)
    {
        services.AddMediator()
                .AddPerformancePipeline()
                .AddValidatorPipeline();
    }
}
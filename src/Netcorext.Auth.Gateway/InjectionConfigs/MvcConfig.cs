namespace Netcorext.Auth.Gateway.InjectionConfigs;

[Injection]
public class MvcConfig
{
    public MvcConfig(IServiceCollection services)
    {
        services.AddCors();
    }
}

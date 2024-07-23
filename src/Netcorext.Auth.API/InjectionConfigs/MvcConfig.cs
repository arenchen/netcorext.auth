namespace Netcorext.Auth.API.InjectionConfigs;

[Injection]
public class MvcConfig
{
    public MvcConfig(IServiceCollection services)
    {
        services.AddCors();
    }
}

namespace Netcorext.Auth.Authentication.InjectionConfigs;

[Injection]
public class MvcConfig
{
    public MvcConfig(IServiceCollection services)
    {
        services.AddCors();
    }
}
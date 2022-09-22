namespace Netcorext.Auth.API.InjectionConfigs;

[Injection]
public class MvcConfig
{
    public MvcConfig(IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors();
    }
}
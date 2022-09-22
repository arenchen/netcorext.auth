namespace Netcorext.Auth.API.InjectionConfigs;

[Injection]
public class AuthConfig
{
    public AuthConfig(IServiceCollection services)
    {
        services.AddJwtAuthentication();
    }
}
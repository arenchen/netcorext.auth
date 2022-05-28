using Netcorext.Extensions.DependencyInjection;

namespace Netcorext.Auth.Authorization.InjectionConfigs;

[Injection]
public class AuthConfig
{
    public AuthConfig(IServiceCollection services)
    {
        services.AddJwtAuthentication();
    }
}
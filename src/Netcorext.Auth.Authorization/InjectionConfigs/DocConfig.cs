using Netcorext.Extensions.DependencyInjection;

namespace Netcorext.Auth.Authorization.InjectionConfigs;

[Injection]
public class DocConfig
{
    public DocConfig(IServiceCollection services)
    {
        services.AddSwaggerGen();
    }
}
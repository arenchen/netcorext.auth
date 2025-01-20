using Netcorext.Auth.Authorization.Settings;

namespace Netcorext.Auth.Authorization.InjectionConfigs;

[Injection]
public class DocConfig
{
    public DocConfig(IServiceCollection services, IConfiguration configuration)
    {
        var cfg = configuration.Get<ConfigSettings>()!;
        services.AddSwaggerGenWithAuth(new Uri(cfg.Document.TokenUrl, UriKind.RelativeOrAbsolute));
    }
}

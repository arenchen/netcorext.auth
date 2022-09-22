namespace Netcorext.Auth.Authorization.InjectionConfigs;

[Injection]
public class DocConfig
{
    public DocConfig(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerGenWithAuth(new Uri(configuration["Document:TokenUrl"], UriKind.RelativeOrAbsolute));
    }
}
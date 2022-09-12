using Netcorext.Extensions.DependencyInjection;
using Netcorext.Extensions.Swagger.Extensions;

namespace Netcorext.Auth.Authorization.InjectionConfigs;

[Injection]
public class DocConfig
{
    public DocConfig(IServiceCollection services, IConfiguration configuration)
    {
        var id = configuration["Id"];
        var tokenUrl = configuration["OAuth:Document:AccessTokenUrl"];
        tokenUrl = tokenUrl.Replace("$id", id).ToLower();
        services.AddSwaggerGenWithAuth(new Uri(tokenUrl, UriKind.RelativeOrAbsolute), options => options.CustomSchemaIds(t => t.FullName));
    }
}
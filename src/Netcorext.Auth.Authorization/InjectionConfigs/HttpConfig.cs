using Netcorext.Extensions.DependencyInjection;
using Netcorext.Logging.HttpClientLogger;

namespace Netcorext.Auth.Authorization.InjectionConfigs;

[Injection]
public class HttpConfig
{
    public HttpConfig(IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddHttpClient("")
                .AddLoggingHttpMessage();
    }
}
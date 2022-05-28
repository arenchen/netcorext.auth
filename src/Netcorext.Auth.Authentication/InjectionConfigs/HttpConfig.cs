using Netcorext.Extensions.DependencyInjection;
using Netcorext.Logging.HttpClientLogger;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

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
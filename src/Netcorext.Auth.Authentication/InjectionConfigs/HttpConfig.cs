using Netcorext.Auth.Authentication.Settings;
using Netcorext.Logging.HttpClientLogger;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

[Injection]
public class HttpConfig
{
    public HttpConfig(IServiceCollection services, IConfiguration configuration)
    {
        var cfg = configuration.Get<ConfigSettings>()!;

        services.AddContextState();
        services.AddHttpClient("")
                .AddRequestId(cfg.AppSettings.RequestIdHeaderName, cfg.AppSettings.RequestIdFromHeaderNames)
                .AddLoggingHttpMessage();
    }
}

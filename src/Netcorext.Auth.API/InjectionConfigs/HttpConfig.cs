using Netcorext.Logging.HttpClientLogger;

namespace Netcorext.Auth.API.InjectionConfigs;

[Injection]
public class HttpConfig
{
    public HttpConfig(IServiceCollection services, IConfiguration configuration)
    {
        var requestIdHeaderName = configuration.GetValue<string>("AppSettings:RequestIdHeaderName");
        var requestIdFromHeaderNames = configuration.GetSection("AppSettings:RequestIdFromHeaderNames").Get<string[]>();

        services.AddContextState();

        services.AddHttpClient("")
                .AddRequestId(requestIdHeaderName, requestIdFromHeaderNames)
                .AddLoggingHttpMessage();
    }
}

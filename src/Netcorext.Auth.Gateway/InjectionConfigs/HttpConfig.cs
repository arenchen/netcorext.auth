using Netcorext.Logging.HttpClientLogger;

namespace Netcorext.Auth.Gateway.InjectionConfigs;

[Injection]
public class HttpConfig
{
    public HttpConfig(IServiceCollection services, IConfiguration configuration)
    {
        var requestIdHeaderName = configuration.GetValue<string>("AppSettings:RequestIdHeaderName");
        var requestIdFromHeaderNames = configuration.GetSection("AppSettings:RequestIdFromHeaderNames").Get<string[]>();

        services.AddHttpContextAccessor();

        services.AddHttpClient("")
                .AddRequestId(requestIdHeaderName, requestIdFromHeaderNames)
                .AddLoggingHttpMessage();
    }
}

using Microsoft.AspNetCore.HttpLogging;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

[Injection]
public class MvcConfig
{
    public MvcConfig(IServiceCollection services)
    {
        services.AddHttpLogging(options =>
                                {
                                    options.LoggingFields = HttpLoggingFields.All;
                                    options.RequestBodyLogLimit = 100 * 1024;
                                    options.ResponseBodyLogLimit = 100 * 1024;
                                });

        services.AddCors();
    }
}

using Microsoft.AspNetCore.HttpLogging;
using Netcorext.Auth.Gateway.Settings;
using Netcorext.Configuration.ConfigSections;
using Netcorext.Extensions.Linq;

namespace Netcorext.Auth.Gateway.InjectionConfigs;

[Injection]
public class MvcConfig
{
    public MvcConfig(IServiceCollection services, IConfiguration configuration)
    {
        var config = configuration.Get<ConfigSettings>()!;

        services.AddHttpLogging(options =>
                                {
                                    options.LoggingFields = config.HttpLoggingOptions.LoggingFields switch
                                                            {
                                                                HttpLoggingSection.HttpLoggingFields.None => HttpLoggingFields.None,
                                                                HttpLoggingSection.HttpLoggingFields.RequestPath => HttpLoggingFields.RequestPath,
                                                                HttpLoggingSection.HttpLoggingFields.RequestQuery => HttpLoggingFields.RequestQuery,
                                                                HttpLoggingSection.HttpLoggingFields.RequestProtocol => HttpLoggingFields.RequestProtocol,
                                                                HttpLoggingSection.HttpLoggingFields.RequestMethod => HttpLoggingFields.RequestMethod,
                                                                HttpLoggingSection.HttpLoggingFields.RequestScheme => HttpLoggingFields.RequestScheme,
                                                                HttpLoggingSection.HttpLoggingFields.ResponseStatusCode => HttpLoggingFields.ResponseStatusCode,
                                                                HttpLoggingSection.HttpLoggingFields.RequestHeaders => HttpLoggingFields.RequestHeaders,
                                                                HttpLoggingSection.HttpLoggingFields.ResponseHeaders => HttpLoggingFields.ResponseHeaders,
                                                                HttpLoggingSection.HttpLoggingFields.RequestTrailers => HttpLoggingFields.RequestTrailers,
                                                                HttpLoggingSection.HttpLoggingFields.ResponseTrailers => HttpLoggingFields.ResponseTrailers,
                                                                HttpLoggingSection.HttpLoggingFields.RequestBody => HttpLoggingFields.RequestBody,
                                                                HttpLoggingSection.HttpLoggingFields.ResponseBody => HttpLoggingFields.ResponseBody,
                                                                HttpLoggingSection.HttpLoggingFields.RequestProperties => HttpLoggingFields.RequestProperties,
                                                                HttpLoggingSection.HttpLoggingFields.RequestPropertiesAndHeaders => HttpLoggingFields.RequestPropertiesAndHeaders,
                                                                HttpLoggingSection.HttpLoggingFields.ResponsePropertiesAndHeaders => HttpLoggingFields.ResponsePropertiesAndHeaders,
                                                                HttpLoggingSection.HttpLoggingFields.Request => HttpLoggingFields.Request,
                                                                HttpLoggingSection.HttpLoggingFields.Response => HttpLoggingFields.Response,
                                                                HttpLoggingSection.HttpLoggingFields.All => HttpLoggingFields.All,
                                                                _ => HttpLoggingFields.RequestPropertiesAndHeaders | HttpLoggingFields.ResponsePropertiesAndHeaders
                                                            };

                                    config.HttpLoggingOptions.RequestHeaders.ForEach(t => options.RequestHeaders.Add(t));
                                    config.HttpLoggingOptions.ResponseHeaders.ForEach(t => options.ResponseHeaders.Add(t));

                                    options.RequestBodyLogLimit = config.HttpLoggingOptions.RequestBodyLogLimit;
                                    options.ResponseBodyLogLimit = config.HttpLoggingOptions.ResponseBodyLogLimit;
                                });

        services.AddCors();
    }
}

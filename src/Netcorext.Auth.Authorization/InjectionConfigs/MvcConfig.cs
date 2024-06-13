using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace Netcorext.Auth.Authorization.InjectionConfigs;

[Injection]
public class MvcConfig
{
    public MvcConfig(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpLogging(options =>
                                {
                                    options.LoggingFields = HttpLoggingFields.All;
                                    options.RequestBodyLogLimit = 100 * 1024;
                                    options.ResponseBodyLogLimit = 100 * 1024;
                                });

        services.AddApiVersioning(options =>
                                  {
                                      options.ReportApiVersions = true;
                                      options.AssumeDefaultVersionWhenUnspecified = true;
                                      options.DefaultApiVersion = new ApiVersion(1, 0);
                                  });

        services.AddVersionedApiExplorer(options =>
                                         {
                                             options.GroupNameFormat = "'v'VVV";
                                             options.SubstituteApiVersionInUrl = true;
                                         });

        services.AddControllers(options =>
                                {
                                    var routePrefix = configuration.GetValue("Route:RoutePrefix", "");
                                    var versionRoute = configuration.GetValue("Route:VersionRoute", "");
                                    var routePattern = routePrefix + versionRoute;
                                    if (!string.IsNullOrWhiteSpace(routePattern)) options.UseRoutePrefix(routePattern);
                                    options.AddFromFormOrBodyBinderProvider();
                                })
                .AddJsonOptions(options =>
                                {
                                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                                    options.JsonSerializerOptions.WriteIndented = false;
                                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                                });

        services.AddCors();
    }
}

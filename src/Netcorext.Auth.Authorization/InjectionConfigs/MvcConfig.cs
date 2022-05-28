using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Netcorext.Extensions.DependencyInjection;

namespace Netcorext.Auth.Authorization.InjectionConfigs;

[Injection]
public class MvcConfig
{
    public MvcConfig(IServiceCollection services, IConfiguration configuration)
    {
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
                                    var id = configuration.GetValue("Id", "");
                                    var routePrefix = configuration.GetValue("AppSettings:RoutePrefix", "");
                                    var routePrefixValue = routePrefix.Replace("$id", id).ToLower();
                                    var versionRoute = configuration.GetValue("AppSettings:VersionRoute", "");
                                    var routePattern = routePrefixValue + versionRoute;
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
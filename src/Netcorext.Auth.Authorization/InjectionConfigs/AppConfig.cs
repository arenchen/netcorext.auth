using Microsoft.Extensions.Options;
using Netcorext.Auth.Authorization.Services.Authorization;
using Netcorext.Auth.Authorization.Settings;
using Netcorext.Extensions.AspNetCore.Middlewares;
using Netcorext.Extensions.DependencyInjection;
using Netcorext.Extensions.Swagger.Extensions;

namespace Netcorext.Auth.Authorization.InjectionConfigs;

[Injection]
public class AppConfig
{
    public AppConfig(WebApplication app)
    {
        var config = app.Services.GetRequiredService<IOptions<ConfigSettings>>().Value;

        app.UseCors(b =>
                    {
                        b.SetIsOriginAllowed(host =>
                                             {
                                                 var allowList = app.Configuration.GetValue("AllowedHosts", string.Empty)
                                                                    .Split(";", StringSplitOptions.RemoveEmptyEntries);

                                                 return allowList.Any(h => h.Equals(host, StringComparison.OrdinalIgnoreCase) || h == "*");
                                             })
                         .AllowAnyHeader()
                         .AllowAnyMethod()
                         .AllowCredentials();
                    });

        app.UseMiddleware<CustomExceptionMiddleware>();
        app.UseJwtAuthentication();

        if (app.Environment.IsDevelopment())
        {
            var docRoute = config.DocumentUrl.Replace("$id", config.Id).ToLower();

            app.UseSwagger(typeof(ConfigSettings).Assembly.GetName().Name!,
                           docRoute,
                           docRoute + "/{*remainder}",
                           docRoute + "/{documentName}/swagger.json");
        }

        app.UseSimpleHealthChecks(provider =>
                                  {
                                      var routePrefixValue = config.AppSettings.HealthRoute?.Replace("$id", config.Id).ToLower() ?? "";

                                      return routePrefixValue;
                                  });

        app.MapGrpcService<AuthorizationServiceFacade>();
        app.MapControllers();

        app.RegisterPermissionEndpoints((_, registerConfig) =>
                                        {
                                            registerConfig.RouteGroupName = config.Id;
                                            registerConfig.RouteServiceUrl = config.Services["Authentication"].Url;
                                            registerConfig.HttpBaseUrl = config.AppSettings.HttpBaseUrl;
                                            registerConfig.Http2BaseUrl = config.AppSettings.Http2BaseUrl;
                                            registerConfig.ForwarderRequestVersion = config.AppSettings.ForwarderRequestVersion;
                                            registerConfig.ForwarderHttpVersionPolicy = config.AppSettings.ForwarderHttpVersionPolicy;
                                            registerConfig.ForwarderActivityTimeout = config.AppSettings.ForwarderActivityTimeout;
                                            registerConfig.ForwarderAllowResponseBuffering = config.AppSettings.ForwarderAllowResponseBuffering;
                                        });

        app.Run();
    }
}
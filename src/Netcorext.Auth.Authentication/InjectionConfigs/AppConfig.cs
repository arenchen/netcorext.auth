using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Middlewares;
using Netcorext.Auth.Authentication.Services.Permission;
using Netcorext.Auth.Authentication.Services.Route;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Extensions.DependencyInjection;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

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

        app.UseMiddleware<TokenMiddleware>();
        app.UseJwtAuthentication();
        app.UseMiddleware<PermissionMiddleware>();

        app.MapReverseProxy();

        app.UseSimpleHealthChecks(provider =>
                                  {
                                      var routePrefixValue = config.AppSettings.HealthRoute?.Replace("$id", config.Id).ToLower() ?? "";

                                      return routePrefixValue;
                                  });

        app.MapGrpcService<RouteServiceFacade>();
        app.MapGrpcService<PermissionServiceFacade>();

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
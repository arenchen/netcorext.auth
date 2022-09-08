using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Services.Client;
using Netcorext.Auth.API.Services.Role;
using Netcorext.Auth.API.Services.User;
using Netcorext.Auth.API.Settings;
using Netcorext.Extensions.AspNetCore.Middlewares;
using Netcorext.Extensions.DependencyInjection;

namespace Netcorext.Auth.API.InjectionConfigs;

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

        app.UseSimpleHealthChecks(provider =>
                                  {
                                      var routePrefixValue = config.AppSettings.HealthRoute?.Replace("$id", config.Id).ToLower() ?? "";

                                      return routePrefixValue;
                                  });

        app.MapGrpcService<ClientServiceFacade>();
        app.MapGrpcService<RoleServiceFacade>();
        app.MapGrpcService<UserServiceFacade>();

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
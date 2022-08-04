using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Middlewares;
using Netcorext.Auth.Authentication.Services.Permission;
using Netcorext.Auth.Authentication.Services.Route;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Extensions.DependencyInjection;
using Netcorext.Mediator;

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

        app.RegisterPermissionEndpoints((provider, endpoints) =>
                                        {
                                            var request = new RegisterRoute
                                                          {
                                                              Groups = endpoints.GroupBy(t => t.Protocol)
                                                                                .Select(t => new RegisterRoute.RouteGroup
                                                                                             {
                                                                                                 Name = config.Id + " - " + t.Key,
                                                                                                 BaseUrl = HttpProtocols.Http2.ToString().Equals(t.Key, StringComparison.OrdinalIgnoreCase)
                                                                                                               ? config.AppSettings.Http2BaseUrl
                                                                                                               : config.AppSettings.HttpBaseUrl,
                                                                                                 ForwarderRequestVersion = config.AppSettings.ForwarderRequestVersion,
                                                                                                 ForwarderHttpVersionPolicy = config.AppSettings.ForwarderHttpVersionPolicy,
                                                                                                 ForwarderActivityTimeout = config.AppSettings.ForwarderActivityTimeout,
                                                                                                 ForwarderAllowResponseBuffering = config.AppSettings.ForwarderAllowResponseBuffering,
                                                                                                 Routes = t.Select(t2 => new RegisterRoute.Route
                                                                                                                         {
                                                                                                                             Protocol = t2.Protocol,
                                                                                                                             HttpMethod = t2.HttpMethod,
                                                                                                                             RelativePath = t2.RelativePath,
                                                                                                                             Template = t2.Template,
                                                                                                                             FunctionId = t2.FunctionId,
                                                                                                                             NativePermission = t2.NativePermission,
                                                                                                                             AllowAnonymous = t2.AllowAnonymous,
                                                                                                                             Tag = t2.Tag,
                                                                                                                             RouteValues = t2.RouteValues
                                                                                                                                             .Select(t3 => new RegisterRoute.RouteValue
                                                                                                                                                           {
                                                                                                                                                               Key = t3.Key,
                                                                                                                                                               Value = t3.Value
                                                                                                                                                           })
                                                                                                                                             .ToArray()
                                                                                                                         })
                                                                                                           .ToArray()
                                                                                             })
                                                                                .ToArray()
                                                          };

                                            var dispatcher = provider.GetRequiredService<IDispatcher>();

                                            dispatcher.SendAsync(request).GetAwaiter().GetResult();
                                        });

        app.Run();
    }
}
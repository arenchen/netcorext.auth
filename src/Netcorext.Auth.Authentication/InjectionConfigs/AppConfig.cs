using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Middlewares;
using Netcorext.Auth.Authentication.Services.Permission;
using Netcorext.Auth.Authentication.Services.Route;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.EntityFramework.UserIdentityPattern.AspNetCore;
using Netcorext.Extensions.DependencyInjection;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

[Injection]
public class AppConfig
{
    public AppConfig(WebApplication app)
    {
        var config = app.Services.GetRequiredService<IOptions<ConfigSettings>>().Value;
        
        app.EnsureCreateDatabase<Domain.Entities.Route>();
        app.UseMiddleware<TokenMiddleware>();
        app.UseJwtAuthentication();
        app.UseMiddleware<PermissionMiddleware>();
        app.MapReverseProxy();

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

        app.UseSimpleHealthChecks(provider =>
                                  {
                                      var routePrefixValue = config.AppSettings.RoutePrefix?.Replace("$id", config.Id).ToLower() ?? "";
        
                                      return "/" + routePrefixValue + config.AppSettings.HealthRoute;
                                  });
        
        app.MapGrpcService<RouteServiceFacade>();
        app.MapGrpcService<PermissionServiceFacade>();

        app.RegisterPermissionEndpoints((provider, endpoints) =>
                                        {
                                            var request = new RegisterRoute
                                                          {
                                                              Routes = endpoints.Select(t => new RegisterRoute.Route
                                                                                             {
                                                                                                 Group = t.Group,
                                                                                                 Protocol = t.Protocol,
                                                                                                 HttpMethod = t.HttpMethod,
                                                                                                 BaseUrl = HttpProtocols.Http2.ToString().Equals(t.Protocol, StringComparison.OrdinalIgnoreCase)
                                                                                                               ? config.AppSettings.Http2BaseUrl
                                                                                                               : config.AppSettings.HttpBaseUrl,
                                                                                                 RelativePath = t.RelativePath,
                                                                                                 Template = t.Template,
                                                                                                 FunctionId = t.FunctionId,
                                                                                                 NativePermission = t.NativePermission,
                                                                                                 AllowAnonymous = t.AllowAnonymous,
                                                                                                 Tag = t.Tag,
                                                                                                 RouteValues = t.RouteValues
                                                                                                                .Select(t2 => new RegisterRoute.RouteValue
                                                                                                                              {
                                                                                                                                  Key = t2.Key,
                                                                                                                                  Value = t2.Value
                                                                                                                              })
                                                                                             })
                                                          };

                                            var dispatcher = provider.GetRequiredService<IDispatcher>();
                                            dispatcher.SendAsync(request).GetAwaiter().GetResult();
                                        });

        app.Run();
    }
}
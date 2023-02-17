using Mapster;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Middlewares;
using Netcorext.Auth.Authentication.Services.Permission;
using Netcorext.Auth.Authentication.Services.Route;
using Netcorext.Auth.Authentication.Services.Route.Commands;
using Netcorext.Auth.Authentication.Services.Token;
using Netcorext.Auth.Authentication.Settings;
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
        app.UseMiddleware<MaintainMiddleware>();
        app.UseMiddleware<PermissionMiddleware>();

        app.MapReverseProxy();

        app.UseSimpleHealthChecks(_ => (config.Route.RoutePrefix + config.Route.HealthRoute).ToLower());

        app.MapGrpcService<RouteServiceFacade>();
        app.MapGrpcService<PermissionValidationServiceFacade>();
        app.MapGrpcService<TokenValidationServiceFacade>();

        app.RegisterPermissionEndpoints((_, registerConfig) =>
                                        {
                                            config.AppSettings.RegisterConfig?.Adapt(registerConfig);
                                            registerConfig.RouteGroupName = config.Id;
                                            registerConfig.RouteServiceUrl = config.Services["Netcorext.Auth.Authentication"].Url;
                                        }, (provider, registerConfig, endpoints) =>
                                           {
                                               var request = new RegisterRoute
                                                             {
                                                                 Groups = endpoints.GroupBy(t => t.Protocol)
                                                                                   .Select(t => new RegisterRoute.RouteGroup
                                                                                                {
                                                                                                    Name = config.Id + " - " + t.Key,
                                                                                                    BaseUrl = HttpProtocols.Http2.ToString().Equals(t.Key, StringComparison.OrdinalIgnoreCase)
                                                                                                                  ? registerConfig.Http2BaseUrl
                                                                                                                  : registerConfig.HttpBaseUrl,
                                                                                                    ForwarderRequestVersion = registerConfig.ForwarderRequestVersion,
                                                                                                    ForwarderHttpVersionPolicy = registerConfig.ForwarderHttpVersionPolicy,
                                                                                                    ForwarderActivityTimeout = registerConfig.ForwarderActivityTimeout,
                                                                                                    ForwarderAllowResponseBuffering = registerConfig.ForwarderAllowResponseBuffering,
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
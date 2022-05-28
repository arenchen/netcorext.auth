using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authorization.Services.Authorization;
using Netcorext.Auth.Authorization.Settings;
using Netcorext.Auth.Extensions;
using Netcorext.Auth.Protobufs;
using Netcorext.EntityFramework.UserIdentityPattern.AspNetCore;
using Netcorext.Extensions.AspNetCore.Middlewares;
using Netcorext.Extensions.DependencyInjection;

namespace Netcorext.Auth.Authorization.InjectionConfigs;

[Injection]
public class AppConfig
{
    public AppConfig(WebApplication app)
    {
        var config = app.Services.GetRequiredService<IOptions<ConfigSettings>>().Value;
        
        app.EnsureCreateDatabase<Domain.Entities.Route>();
        app.UseMiddleware<CustomExceptionMiddleware>();
        app.UseJwtAuthentication();

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

        if (app.Environment.IsDevelopment() || app.Environment.IsLocalhost())
        {
            var docRoute = config.DocumentUrl.Replace("$id", config.Id).ToLower();

            app.MapSwagger(docRoute + "/{documentName}/swagger.json");
            // app.UseSwagger(options =>
            //                {
            //                    options.RouteTemplate = docRoute + "/{documentName}/swagger.json";
            //                });
            app.UseSwaggerUI(options =>
                             {
                                 options.SwaggerEndpoint("v1/swagger.json", typeof(ConfigSettings).Assembly.GetName().Name);
                                 options.RoutePrefix = docRoute;
                             });
        }

        app.UseSimpleHealthChecks(provider =>
                                  {
                                      var routePrefixValue = config.AppSettings.RoutePrefix?.Replace("$id", config.Id).ToLower() ?? "";

                                      return "/" + routePrefixValue + config.AppSettings.HealthRoute;
                                  });

        app.MapGrpcService<AuthorizationServiceFacade>();
        app.MapControllers();

        app.RegisterPermissionEndpoints((provider, endpoints) =>
                                        {
                                            var request = new RegisterRouteRequest
                                                          {
                                                              Routes =
                                                              {
                                                                  endpoints.Select(t => new RegisterRouteRequest.Types.Route
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
                                                                                            NativePermission = t.NativePermission.ToProtobufPermissionType(),
                                                                                            AllowAnonymous = t.AllowAnonymous,
                                                                                            Tag = t.Tag,
                                                                                            RouteValues =
                                                                                            {
                                                                                                t.RouteValues.Select(t2 => new RegisterRouteRequest.Types.RouteValue
                                                                                                                           {
                                                                                                                               Key = t2.Key,
                                                                                                                               Value = t2.Value
                                                                                                                           })
                                                                                            }
                                                                                        })
                                                              }
                                                          };
        
                                            var routeService = provider.GetRequiredService<RouteService.RouteServiceClient>();
        
                                            routeService.RegisterRoute(request);
                                        });

        app.Run();
    }
}
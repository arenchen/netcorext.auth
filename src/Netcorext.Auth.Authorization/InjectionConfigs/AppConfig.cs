using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authorization.Services.Authorization;
using Netcorext.Auth.Authorization.Settings;
using Netcorext.Auth.Enums;
using Netcorext.Extensions.AspNetCore.Middlewares;
using Netcorext.Extensions.DependencyInjection;

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

            app.MapSwagger(docRoute + "/{documentName}/swagger.json");

            app.UseSwaggerUI(options =>
                             {
                                 options.SwaggerEndpoint("v1/swagger.json", typeof(ConfigSettings).Assembly.GetName().Name);
                                 options.RoutePrefix = docRoute;
                             });
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
                                        },
                                        otherPermissionEndpoints: new[]
                                                                  {
                                                                      new PermissionEndpoint
                                                                      {
                                                                          Group = config.Id,
                                                                          Protocol = HttpProtocols.Http1.ToString(),
                                                                          HttpMethod = "GET",
                                                                          BaseUrl = config.AppSettings.HttpBaseUrl.TrimEnd(char.Parse("/")),
                                                                          RelativePath = config.DocumentUrl.Replace("$id", config.Id).ToLower().Trim(char.Parse("/")),
                                                                          Template = config.DocumentUrl.Replace("$id", config.Id).ToLower().Trim(char.Parse("/")),
                                                                          RouteValues = new Dictionary<string, string?>(),
                                                                          FunctionId = "DOC",
                                                                          NativePermission = PermissionType.All,
                                                                          AllowAnonymous = true,
                                                                          Tag = null
                                                                      },
                                                                      new PermissionEndpoint
                                                                      {
                                                                          Group = config.Id,
                                                                          Protocol = HttpProtocols.Http1.ToString(),
                                                                          HttpMethod = "GET",
                                                                          BaseUrl = config.AppSettings.HttpBaseUrl.TrimEnd(char.Parse("/")),
                                                                          RelativePath = config.AppSettings.HealthRoute!.Replace("$id", config.Id).ToLower().Trim(char.Parse("/")),
                                                                          Template = config.AppSettings.HealthRoute.Replace("$id", config.Id).ToLower().Trim(char.Parse("/")),
                                                                          RouteValues = new Dictionary<string, string?>(),
                                                                          FunctionId = "HEALTH",
                                                                          NativePermission = PermissionType.All,
                                                                          AllowAnonymous = true,
                                                                          Tag = null
                                                                      }
                                                                  });

        app.Run();
    }
}
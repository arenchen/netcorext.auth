using Mapster;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authorization.Services.Authorization;
using Netcorext.Auth.Authorization.Settings;
using Netcorext.Extensions.AspNetCore.Middlewares;
using Netcorext.Logging.AspNetCoreLogger;

namespace Netcorext.Auth.Authorization.InjectionConfigs;

[Injection]
public class AppConfig
{
    public AppConfig(WebApplication app)
    {
        var config = app.Services.GetRequiredService<IOptions<ConfigSettings>>().Value;

        app.UseMiddleware<CustomExceptionMiddleware>();
        app.UseRequestId(config.AppSettings.RequestIdHeaderName, config.AppSettings.RequestIdFromHeaderNames);

        if (config.AppSettings.EnableAspNetCoreLogger)
            app.UseAspNetCoreLogger();

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


        app.UseDefaultHealthChecks(config.Route.RoutePrefix + config.Route.HealthRoute, config.Route.HealthRoute);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger((config.Route.RoutePrefix + config.Document.Url).ToLower());
        }

        app.MapControllers();
        app.MapGrpcService<AuthorizationServiceFacade>();

        app.RegisterPermissionEndpoints((_, registerConfig) =>
                                        {
                                            config.AppSettings.RegisterConfig?.Adapt(registerConfig);
                                            registerConfig.RouteGroupName = config.Id;
                                            registerConfig.RouteServiceUrl = config.Services["Netcorext.Auth.Gateway"].Url;
                                        });

        app.Run();
    }
}

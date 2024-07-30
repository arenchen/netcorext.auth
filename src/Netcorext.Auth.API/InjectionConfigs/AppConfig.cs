using Mapster;
using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Services.Blocked;
using Netcorext.Auth.API.Services.Client;
using Netcorext.Auth.API.Services.Maintenance;
using Netcorext.Auth.API.Services.Permission;
using Netcorext.Auth.API.Services.Role;
using Netcorext.Auth.API.Services.User;
using Netcorext.Auth.API.Settings;
using Netcorext.Extensions.AspNetCore.Middlewares;
using Netcorext.Logging.AspNetCoreLogger;

namespace Netcorext.Auth.API.InjectionConfigs;

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

        app.UseDefaultHealthChecks(_ => (config.Route.RoutePrefix + config.Route.HealthRoute).ToLower());

        app.MapGrpcService<ClientServiceFacade>();
        app.MapGrpcService<PermissionServiceFacade>();
        app.MapGrpcService<RoleServiceFacade>();
        app.MapGrpcService<UserServiceFacade>();
        app.MapGrpcService<BlockedServiceFacade>();
        app.MapGrpcService<MaintenanceServiceFacade>();

        app.RegisterPermissionEndpoints((_, registerConfig) =>
                                        {
                                            config.AppSettings.RegisterConfig?.Adapt(registerConfig);
                                            registerConfig.RouteGroupName = config.Id;
                                            registerConfig.RouteServiceUrl = config.Services["Netcorext.Auth.Gateway"].Url;
                                        });

        app.Run();
    }
}

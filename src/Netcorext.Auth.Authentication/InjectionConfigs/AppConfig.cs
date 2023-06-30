using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Middlewares;
using Netcorext.Auth.Authentication.Services.Maintenance;
using Netcorext.Auth.Authentication.Services.Permission;
using Netcorext.Auth.Authentication.Services.Route;
using Netcorext.Auth.Authentication.Services.Token;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Extensions.AspNetCore.Middlewares;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

[Injection]
public class AppConfig
{
    public AppConfig(WebApplication app)
    {
        var config = app.Services.GetRequiredService<IOptions<ConfigSettings>>().Value;

        app.UseMiddleware<CustomExceptionMiddleware>();
        app.UseRequestId(config.AppSettings.RequestIdHeaderName, config.AppSettings.RequestIdFromHeaderNames);

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

        app.UseSimpleHealthChecks(_ => (config.Route.RoutePrefix + config.Route.HealthRoute).ToLower());

        app.MapGrpcService<RouteServiceFacade>();
        app.MapGrpcService<PermissionValidationServiceFacade>();
        app.MapGrpcService<MaintenanceServiceFacade>();
        app.MapGrpcService<TokenValidationServiceFacade>();

        app.MapReverseProxy();

        app.Run();
    }
}
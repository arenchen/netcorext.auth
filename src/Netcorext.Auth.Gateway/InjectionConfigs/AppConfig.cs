using Microsoft.Extensions.Options;
using Netcorext.Auth.Gateway.Services.Route;
using Netcorext.Auth.Gateway.Settings;
using Netcorext.Extensions.AspNetCore.Middlewares;
using Netcorext.Logging.AspNetCoreLogger;

namespace Netcorext.Auth.Gateway.InjectionConfigs;

[Injection]
public class AppConfig
{
    public AppConfig(WebApplication app)
    {
        var config = app.Services.GetRequiredService<IOptions<ConfigSettings>>().Value;

        app.UseMiddleware<CustomExceptionMiddleware>();
        app.UseRequestId(config.AppSettings.RequestIdHeaderName, config.AppSettings.RequestIdFromHeaderNames);
        app.UseAspNetCoreLogger();
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

        app.UseJwtAuthentication();
        app.UseDefaultHealthChecks(_ => (config.Route.RoutePrefix + config.Route.HealthRoute).ToLower());
        app.MapGrpcService<RouteServiceFacade>();
        app.MapReverseProxy();

        app.Run();
    }
}

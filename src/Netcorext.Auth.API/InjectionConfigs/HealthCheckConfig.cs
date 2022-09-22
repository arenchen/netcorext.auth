using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Settings;
using Netcorext.Configuration.Extensions;
using Netcorext.Diagnostics.HealthChecks.PostgreSql;
using Netcorext.Diagnostics.HealthChecks.Redis;

namespace Netcorext.Auth.API.InjectionConfigs;

[Injection]
public class HealthCheckConfig
{
    public HealthCheckConfig(IServiceCollection services)
    {
        services.AddHealthChecks()
                .AddVersion()
                .AddPostgreSql(provider =>
                               {
                                   var config = provider.GetRequiredService<IOptions<ConfigSettings>>().Value;

                                   return new PostgreSqlHealthCheckOptions
                                          {
                                              Connection = config.Connections.RelationalDb.GetDefault()!.Connection
                                          };
                               }, "Postgresql")
                .AddRedis(provider =>
                          {
                              var config = provider.GetRequiredService<IOptions<ConfigSettings>>().Value;

                              return new RedisHealthCheckOptions
                                     {
                                         Connection = config.Connections.Redis.GetDefault()!.Connection
                                     };
                          }, "Redis");
    }
}
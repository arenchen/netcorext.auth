using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Configuration.Extensions;
using Netcorext.Diagnostics.HealthChecks.MemoryCache;
using Netcorext.Diagnostics.HealthChecks.PostgreSql;
using Netcorext.Diagnostics.HealthChecks.Redis;
using Netcorext.EntityFramework.UserIdentityPattern.Helpers;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

[Injection]
public class HealthCheckConfig
{
    public HealthCheckConfig(IServiceCollection services)
    {
        services.AddHealthChecks()
                .AddVersion()
                .AddMemoryCache(provider =>
                                {
                                    var config = provider.GetRequiredService<IOptions<ConfigSettings>>().Value;

                                    return new MemoryCacheHealthCheckerOptions
                                           {
                                               CheckKeys = config.AppSettings.CheckCacheKeys
                                           };
                                }, "MemoryCache")
                .AddPostgreSql(provider =>
                               {
                                   var config = provider.GetRequiredService<IOptions<ConfigSettings>>().Value;

                                   return new PostgreSqlHealthCheckOptions
                                          {
                                              Connection = DbOptionsHelper.GetConnectionString(config.Connections.RelationalDb.GetDefault().Connection)!
                                          };
                               }, "Postgresql")
                .AddRedis(provider =>
                          {
                              var config = provider.GetRequiredService<IOptions<ConfigSettings>>().Value;

                              return new RedisHealthCheckOptions
                                     {
                                         Connection = config.Connections.Redis.GetDefault().Connection
                                     };
                          }, "Redis");
    }
}

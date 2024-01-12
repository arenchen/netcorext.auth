using FreeRedis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Netcorext.Auth.Authorization.Settings;
using Netcorext.EntityFramework.UserIdentityPattern.AspNetCore;
using Netcorext.Extensions.Redis.Utilities;
using Netcorext.Serialization;

namespace Netcorext.Auth.Authorization.InjectionConfigs;

[Injection]
public class DbConfig
{
    public DbConfig(IServiceCollection services, IConfiguration config)
    {
        var cfg = config.Get<ConfigSettings>();
        var slowCommandLoggingThreshold = cfg.AppSettings.SlowCommandLoggingThreshold;
        var mainDb = cfg.Connections.RelationalDb["Default"];
        var slaveDb = cfg.Connections.RelationalDb["Slave"];
        var redis = cfg.Connections.Redis["Default"];

        services.AddIdentityDbContextPool((_, builder) =>
                                          {
                                              builder.UseNpgsql(mainDb.Connection, options => options.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery));

                                              if (mainDb.EnableDetailedErrors.HasValue)
                                                  builder.EnableDetailedErrors(mainDb.EnableDetailedErrors.Value);

                                              if (mainDb.EnableSensitiveDataLogging.HasValue)
                                                  builder.EnableSensitiveDataLogging(mainDb.EnableSensitiveDataLogging.Value);

                                              if (mainDb.EnableServiceProviderCaching.HasValue)
                                                  builder.EnableServiceProviderCaching(mainDb.EnableServiceProviderCaching.Value);

                                              if (mainDb.EnableThreadSafetyChecks.HasValue)
                                                  builder.EnableThreadSafetyChecks(mainDb.EnableThreadSafetyChecks.Value);
                                          }, mainDb.PoolSize, slowCommandLoggingThreshold);

        services.AddIdentitySlaveDbContextPool((_, builder) =>
                                               {
                                                   builder.UseNpgsql(slaveDb.Connection, options => options.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery));

                                                   if (slaveDb.EnableDetailedErrors.HasValue)
                                                       builder.EnableDetailedErrors(slaveDb.EnableDetailedErrors.Value);

                                                   if (slaveDb.EnableSensitiveDataLogging.HasValue)
                                                       builder.EnableSensitiveDataLogging(slaveDb.EnableSensitiveDataLogging.Value);

                                                   if (slaveDb.EnableServiceProviderCaching.HasValue)
                                                       builder.EnableServiceProviderCaching(slaveDb.EnableServiceProviderCaching.Value);

                                                   if (slaveDb.EnableThreadSafetyChecks.HasValue)
                                                       builder.EnableThreadSafetyChecks(slaveDb.EnableThreadSafetyChecks.Value);
                                               }, slaveDb.PoolSize, slowCommandLoggingThreshold);

        services.TryAddSingleton<RedisClient>(provider =>
                                              {
                                                  var serializer = provider.GetRequiredService<ISerializer>();

                                                  return new RedisClientConnection<RedisClient>(() => new RedisClient(redis.Connection)
                                                                                                      {
                                                                                                          Serialize = serializer.Serialize,
                                                                                                          Deserialize = serializer.Deserialize,
                                                                                                          DeserializeRaw = serializer.Deserialize
                                                                                                      }).Client;
                                              });
    }
}

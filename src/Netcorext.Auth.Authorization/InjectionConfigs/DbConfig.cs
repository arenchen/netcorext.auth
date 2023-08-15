using FreeRedis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authorization.Settings;
using Netcorext.Configuration.Extensions;
using Netcorext.EntityFramework.UserIdentityPattern.AspNetCore;
using Netcorext.Extensions.Redis.Utilities;
using Netcorext.Serialization;

namespace Netcorext.Auth.Authorization.InjectionConfigs;

[Injection]
public class DbConfig
{
    public DbConfig(IServiceCollection services, IConfiguration config)
    {
        var slowCommandLoggingThreshold = config.GetValue<long>("AppSettings:SlowCommandLoggingThreshold", 1000);
        var defaultPoolSize = config.GetValue("Connections:RelationalDb:Default:PoolSize", 1024);
        var slavePoolSize = config.GetValue("Connections:RelationalDb:Slave:PoolSize", 1024);

        services.AddIdentityDbContextPool((provider, builder) =>
                                          {
                                              var cfg = provider.GetRequiredService<IOptions<ConfigSettings>>().Value;

                                              builder.UseNpgsql(cfg.Connections.RelationalDb.GetDefault().Connection);
                                          }, defaultPoolSize, slowCommandLoggingThreshold);

        services.AddIdentitySlaveDbContextPool((provider, builder) =>
                                               {
                                                   var cfg = provider.GetRequiredService<IOptions<ConfigSettings>>().Value;

                                                   builder.UseNpgsql(cfg.Connections.RelationalDb["Slave"].Connection);
                                               }, slavePoolSize, slowCommandLoggingThreshold);

        services.TryAddSingleton<RedisClient>(provider =>
                                              {
                                                  var cfg = provider.GetRequiredService<IOptions<ConfigSettings>>().Value;
                                                  var serializer = provider.GetRequiredService<ISerializer>();

                                                  return new RedisClientConnection<RedisClient>(() => new RedisClient(cfg.Connections.Redis.GetDefault().Connection)
                                                                                                      {
                                                                                                          Serialize = serializer.Serialize,
                                                                                                          Deserialize = serializer.Deserialize,
                                                                                                          DeserializeRaw = serializer.Deserialize
                                                                                                      }).Client;
                                              });
    }
}
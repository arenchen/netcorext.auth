using FreeRedis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Auth.Utilities;
using Netcorext.Configuration.Extensions;
using Netcorext.EntityFramework.UserIdentityPattern.AspNetCore;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

[Injection]
public class DbConfig
{
    public DbConfig(IServiceCollection services, IConfiguration config)
    {
        var slowCommandLoggingThreshold = config.GetValue<long>("AppSettings:SlowCommandLoggingThreshold", 1000);

        services.AddIdentityDbContext((provider, builder) =>
                                      {
                                          var cfg = provider.GetRequiredService<IOptions<ConfigSettings>>().Value;

                                          builder.UseNpgsql(cfg.Connections.RelationalDb.GetDefault()!.Connection);
                                      }, slowCommandLoggingThreshold: slowCommandLoggingThreshold);

        services.TryAddSingleton<RedisClient>(provider =>
                                              {
                                                  var cfg = provider.GetRequiredService<IOptions<ConfigSettings>>().Value;

                                                  return new RedisClientConnection(cfg.Connections.Redis.GetDefault()!.Connection).Client;
                                              });

        services.AddMemoryCache();
    }
}
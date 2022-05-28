using FreeRedis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authorization.Settings;
using Netcorext.Auth.Utilities;
using Netcorext.Configuration.Extensions;
using Netcorext.EntityFramework.UserIdentityPattern.AspNetCore;
using Netcorext.Extensions.DependencyInjection;

namespace Netcorext.Auth.Authorization.InjectionConfigs;

[Injection]
public class DbConfig
{
    public DbConfig(IServiceCollection services)
    {
        services.AddIdentityDbContext((provider, builder) =>
                                      {
                                          var config = provider.GetRequiredService<IOptions<ConfigSettings>>().Value;
                                          builder.UseNpgsql(config.Connections.RelationalDb.GetDefault()!.Connection);
                                      });

        services.TryAddSingleton<RedisClient>(provider =>
                                              {
                                                  var config = provider.GetRequiredService<IOptions<ConfigSettings>>().Value;

                                                  return new RedisClientConnection(config.Connections.Redis.GetDefault()!.Connection).Client;
                                              });
    }
}
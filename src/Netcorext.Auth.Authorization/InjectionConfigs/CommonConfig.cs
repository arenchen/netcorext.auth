using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Netcorext.Algorithms;
using Netcorext.Auth.Authorization.Settings;
using Netcorext.Extensions.DependencyInjection;

namespace Netcorext.Auth.Authorization.InjectionConfigs;

[Injection(int.MaxValue)]
public class CommonSetting
{
    public CommonSetting(IServiceCollection services, IConfiguration configuration)
    {
        IdentityModelEventSource.ShowPII = true;

        services.Configure<ConfigSettings>(configuration);

        services.Configure<AuthOptions>(configuration.GetSection("AuthOptions"));

        services.AddSingleton<ISnowflake>(provider =>
                                          {
                                              var config = provider.GetRequiredService<IOptions<ConfigSettings>>().Value;
                                              var machineId = config.MachineId;
                                              if (machineId == 0) machineId = (uint)new Random().Next(1, 31);

                                              return new SnowflakeJavaScriptSafeInteger(machineId);
                                          });
    }
}
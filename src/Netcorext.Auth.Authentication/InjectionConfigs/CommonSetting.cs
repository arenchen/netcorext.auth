using Microsoft.Extensions.Options;
using Netcorext.Algorithms;
using Netcorext.Auth.Authentication.Settings;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

[Injection(int.MaxValue)]
public class CommonSetting
{
    public CommonSetting(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ConfigSettings>(configuration);
        services.Configure<AuthOptions>(configuration.GetSection("AuthOptions"));

        services.AddSingleton<ISnowflake>(provider =>
                                          {
                                              var config = provider.GetRequiredService<IOptions<ConfigSettings>>().Value;
                                              var machineId = config.MachineId;
                                              if (machineId == 0) machineId = (uint)new Random().Next(1, 31);

                                              return new SnowflakeJavaScriptSafeInteger(machineId);
                                          });

        services.TryAddSystemJsonSerializer();
    }
}

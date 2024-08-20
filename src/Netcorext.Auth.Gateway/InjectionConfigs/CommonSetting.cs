using Microsoft.Extensions.Options;
using Netcorext.Algorithms;
using Netcorext.Auth.Gateway.Settings;
using Netcorext.Extensions.Threading;

namespace Netcorext.Auth.Gateway.InjectionConfigs;

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

        services.AddSingleton<KeyLocker>(provider =>
                                         {
                                             var config = provider.GetRequiredService<IOptions<ConfigSettings>>().Value;
                                             var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                                             var logger = loggerFactory.CreateLogger<KeyLocker>();

                                             return new KeyLocker(logger, maxConcurrent: config.AppSettings.WorkerTaskLimit ?? ConfigSettings.DEFAULT_WORKER_TASK_LIMIT);
                                         });

        services.TryAddSystemJsonSerializer();
    }
}

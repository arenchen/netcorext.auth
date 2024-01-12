using Microsoft.Extensions.Options;
using Netcorext.Auth.Gateway.Settings;
using Netcorext.Configuration.Extensions;

namespace Netcorext.Auth.Gateway.InjectionConfigs;

[Injection]
public class ServiceConfig
{
    public ServiceConfig(IServiceCollection services)
    {
        services.AddMediator()
                .AddRedisQueuing((provider, options) =>
                                 {
                                     var cfg = provider.GetRequiredService<IOptions<ConfigSettings>>().Value;
                                     options.ConnectionString = cfg.Connections.Redis.GetDefault().Connection;
                                 })
                .AddLoggingPipeline()
                .AddPerformancePipeline()
                .AddValidatorPipeline();
    }
}

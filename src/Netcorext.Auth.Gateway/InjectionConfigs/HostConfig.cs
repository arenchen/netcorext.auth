using Serilog;

namespace Netcorext.Auth.Gateway.InjectionConfigs;

[Injection]
public class HostConfig
{
    public HostConfig(IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, configuration) =>
                                          {
                                              var host = context.HostingEnvironment;

                                              configuration.SetBasePath(host.ContentRootPath)
                                                           .AddJsonFile("appsettings.json", false, true)
                                                           .AddJsonFile($"appsettings.{host.EnvironmentName}.json", true, true)
                                                           .AddJsonFile("appsettings.override.json", true, true)
                                                           .AddJsonGzipCompressFile("appsettings.secret", true, true)
                                                           .AddEnvironmentVariables();
                                          })
               .ConfigureLogging(loggingBuilder =>
                                 {
                                     loggingBuilder.ClearProviders();
                                     loggingBuilder.AddSerilog();
                                 })
               .UseSerilog((context, services, configuration) => configuration
                                                                .ReadFrom.Configuration(context.Configuration)
                                                                .ReadFrom.Services(services)
                          );
    }
}

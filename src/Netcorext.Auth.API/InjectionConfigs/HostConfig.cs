using Serilog;

namespace Netcorext.Auth.API.InjectionConfigs;

[Injection]
public class HostConfig
{
    public HostConfig(IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((w, s) =>
                                          {
                                              var host = w.HostingEnvironment;

                                              s.SetBasePath(host.ContentRootPath)
                                               .AddJsonFile("appsettings.json", false, true)
                                               .AddJsonFile($"appsettings.{host.EnvironmentName}.json", true, true)
                                               .AddJsonFile($"appsettings.override.json", true, true)
                                               .AddJsonGzipCompressFile($"appsettings.secret", true, true)
                                               .AddEnvironmentVariables();
                                          })
               .ConfigureLogging(loggingBuilder =>
                                 {
                                     loggingBuilder.ClearProviders();
                                     loggingBuilder.AddSerilog();
                                 })
               .UseSerilog((ctx, provider, lc) =>
                           {
                               Serilog.Debugging.SelfLog.Enable(Console.Error.WriteLine);

                               lc.ReadFrom.Configuration(ctx.Configuration)
                                 .Enrich.FromLogContext()
                                 .WriteTo
                                 .Async(cfg =>
                                        {
#if DEBUG
                                            cfg.Console();
#endif
                                        });
                           });
    }
}
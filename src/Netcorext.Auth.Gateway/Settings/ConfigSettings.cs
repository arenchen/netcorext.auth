using System.Reflection;
using Netcorext.Auth.Extensions.AspNetCore.Settings;
using Netcorext.Configuration;

namespace Netcorext.Auth.Gateway.Settings;

public class ConfigSettings : Config<AppSettings>
{
    public const string CACHE_ROUTE = "Route";
    public const string QUEUES_HEALTH_CHECK_EVENT = "HealthCheckEvent";
    public const string QUEUES_ROUTE_CHANGE_EVENT = "RouteChangeEvent";
}

public class AppSettings
{
    public RegisterConfig? RegisterConfig { get; set; }
    public string LockPrefixKey { get; set; } = Assembly.GetEntryAssembly()!.GetName().Name!.ToLower();
    public long SlowCommandLoggingThreshold { get; set; } = 1000;
    public int? WorkerTaskLimit { get; set; } = 5;
    public int? RetryLimit { get; set; } = 3;
    public string RequestIdHeaderName { get; set; } = "X-Request-Id";
    public string[] RequestIdFromHeaderNames { get; set; } = { "X-Request-Id" };
}

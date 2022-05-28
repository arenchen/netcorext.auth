using Microsoft.AspNetCore.Server.Kestrel.Core;
using Netcorext.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace Netcorext.Auth.Authentication.Settings;

public class ConfigSettings : Config<AppSettings>
{
    public const string QUEUES_TOKEN_REVOKE_EVENT = "TokenRevokeEvent";
    public const string QUEUES_ROUTE_CHANGE_EVENT = "RouteChangeEvent";
    public const string QUEUES_ROLE_CHANGE_EVENT = "RoleChangeEvent";
    public const string QUEUES_HEALTH_CHECK_EVENT = "HealthCheckEvent";
    public const string CACHE_ROUTE = "Route";
    public const string CACHE_TOKEN = "Token";
    public const string CACHE_ROLE_PERMISSION = "RolePermission";
}

public class AppSettings
{
    public string? RoutePrefix { get; set; }
    public string? VersionRoute { get; set; }
    public string? HealthRoute { get; set; }
    public int HealthCheckInterval { get; set; } = 10 * 1000;
    public int HealthCheckTimeout { get; set; } = 15 * 1000;
    public int CacheTokenExpires { get; set; } = 30 * 60 * 1000;
    public bool UseNativeStatus { get; set; }
    public string HttpHostMatchPattern { get; set; } = "*:10080";
    public string Http2HostMatchPattern { get; set; } = "*:10081";
    public string InternalHost { get; set; } = "localhost";
    public string HttpBaseUrl { get; set; } = null!;
    public string Http2BaseUrl { get; set; } = null!;

    public ForwarderRequestConfig ForwarderRequestConfig { get; set; } = new()
                                                                         {
                                                                             Version = new Version(2, 0),
                                                                             VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                                                                         };
}
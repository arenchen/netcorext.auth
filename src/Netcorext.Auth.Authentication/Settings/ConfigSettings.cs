using System.Reflection;
using Netcorext.Auth.Extensions.AspNetCore.Settings;
using Netcorext.Configuration;

namespace Netcorext.Auth.Authentication.Settings;

public class ConfigSettings : Config<AppSettings>
{
    public const string QUEUES_TOKEN_REVOKE_EVENT = "TokenRevokeEvent";
    public const string QUEUES_ROUTE_CHANGE_EVENT = "RouteChangeEvent";
    public const string QUEUES_ROLE_CHANGE_EVENT = "RoleChangeEvent";
    public const string QUEUES_USER_CHANGE_EVENT = "UserChangeEvent";
    public const string QUEUES_USER_ROLE_CHANGE_EVENT = "UserRoleChangeEvent";
    public const string QUEUES_MAINTAIN_CHANGE_EVENT = "MaintainChangeEvent";
    public const string QUEUES_HEALTH_CHECK_EVENT = "HealthCheckEvent";
    public const string CACHE_MAINTAIN = "Maintain";
    public const string CACHE_ROUTE = "Route";
    public const string CACHE_TOKEN = "Token";
    public const string CACHE_ROLE_PERMISSION_RULE = "RolePermissionRule";
    public const string CACHE_ROLE_PERMISSION_CONDITION = "RolePermissionCondition";
    public const string CACHE_USER_PERMISSION_CONDITION = "UserPermissionCondition";
}

public class AppSettings
{
    public RegisterConfig? RegisterConfig { get; set; }
    public string LockPrefixKey { get; set; } = Assembly.GetEntryAssembly()!.GetName().Name!.ToLower();
    public string[]? InternalHost { get; set; }
    public long[]? Owner { get; set; }
    public bool ValidationPassUserId { get; set; }
    public int CacheTokenExpires { get; set; } = 30 * 60 * 1000;
    public bool UseNativeStatus { get; set; }
    public long SlowCommandLoggingThreshold { get; set; } = 1000;
    public int? WorkerTaskLimit { get; set; } = 5;
    public int? RetryLimit { get; set; } = 3;
}
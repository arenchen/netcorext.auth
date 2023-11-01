using Netcorext.Auth.Extensions.AspNetCore.Settings;
using Netcorext.Configuration;

namespace Netcorext.Auth.API.Settings;

public class ConfigSettings : Config<AppSettings>
{
    public const string QUEUES_BLOCKED_IP_CHANGE_EVENT = "BlockedIpChangeEvent";
    public const string QUEUES_CLIENT_CHANGE_EVENT = "ClientChangeEvent";
    public const string QUEUES_CLIENT_ROLE_CHANGE_EVENT = "ClientRoleChangeEvent";
    public const string QUEUES_PERMISSION_CHANGE_EVENT = "PermissionChangeEvent";
    public const string QUEUES_ROLE_CHANGE_EVENT = "RoleChangeEvent";
    public const string QUEUES_TOKEN_REVOKE_EVENT = "TokenRevokeEvent";
    public const string QUEUES_USER_CHANGE_EVENT = "UserChangeEvent";
    public const string QUEUES_USER_ROLE_CHANGE_EVENT = "UserRoleChangeEvent";
}

public class AppSettings
{
    public RegisterConfig? RegisterConfig { get; set; }
    public long SlowCommandLoggingThreshold { get; set; } = 1000;
    public string RequestIdHeaderName { get; set; } = "X-Request-Id";
    public string[] RequestIdFromHeaderNames { get; set; } = { "X-Request-Id" };
}

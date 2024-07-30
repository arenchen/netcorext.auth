using Netcorext.Auth.Extensions.AspNetCore.Settings;
using Netcorext.Configuration;

namespace Netcorext.Auth.Authorization.Settings;

public class ConfigSettings : Config<AppSettings>
{
    public const string QUEUES_TOKEN_REVOKE_EVENT = "TokenRevokeEvent";
    public const string QUEUES_USER_CHANGE_EVENT = "UserChangeEvent";
    public const string QUEUES_USER_SIGN_IN_EVENT = "UserSignInEvent";
    public const string QUEUES_USER_REFRESH_TOKEN_EVENT = "UserRefreshTokenEvent";
    public const string QUEUES_USER_ROLE_CHANGE_EVENT = "UserRoleChangeEvent";

    public const string CACHE_TOKEN_RETAIN = "TokenRetain";
}

public class AppSettings
{
    public RegisterConfig? RegisterConfig { get; set; }
    public long SlowCommandLoggingThreshold { get; set; } = 1000;
    public string RequestIdHeaderName { get; set; } = "X-Request-Id";
    public string[] RequestIdFromHeaderNames { get; set; } = { "X-Request-Id" };
    public bool EnableAspNetCoreLogger { get; set; }
}

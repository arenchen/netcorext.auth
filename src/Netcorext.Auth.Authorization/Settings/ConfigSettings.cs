using Microsoft.AspNetCore.Server.Kestrel.Core;
using Netcorext.Configuration;

namespace Netcorext.Auth.Authorization.Settings;

public class ConfigSettings : Config<AppSettings>
{
    public const string QUEUES_TOKEN_REVOKE_EVENT = "TokenRevokeEvent";
    public const string QUEUES_USER_CHANGE_EVENT = "UserChangeEvent";
    public const string QUEUES_USER_ROLE_CHANGE_EVENT = "UserRoleChangeEvent";
}

public class AppSettings
{
    public string? RoutePrefix { get; set; }
    public string? VersionRoute { get; set; }
    public string? HealthRoute { get; set; }
    public string HttpBaseUrl { get; set; }
    public string Http2BaseUrl { get; set; }
}
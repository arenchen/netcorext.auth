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
    public string HttpBaseUrl { get; set; } = null!;
    public string Http2BaseUrl { get; set; } = null!;
    public string? ForwarderRequestVersion { get; set; }
    public HttpVersionPolicy? ForwarderHttpVersionPolicy { get; set; }
    public TimeSpan? ForwarderActivityTimeout { get; set; }
    public bool? ForwarderAllowResponseBuffering { get; set; }
}
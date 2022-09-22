using Netcorext.Auth.Extensions.AspNetCore.Settings;
using Netcorext.Configuration;

namespace Netcorext.Auth.API.Settings;

public class ConfigSettings : Config<AppSettings>
{
    public const string QUEUES_CLIENT_CHANGE_EVENT = "ClientChangeEvent";
    public const string QUEUES_CLIENT_ROLE_CHANGE_EVENT = "ClientRoleChangeEvent";
    public const string QUEUES_USER_CHANGE_EVENT = "UserChangeEvent";
    public const string QUEUES_USER_ROLE_CHANGE_EVENT = "UserRoleChangeEvent";
    public const string QUEUES_ROLE_CHANGE_EVENT = "RoleChangeEvent";
}

public class AppSettings
{
    public RegisterConfig? RegisterConfig { get; set; }
}
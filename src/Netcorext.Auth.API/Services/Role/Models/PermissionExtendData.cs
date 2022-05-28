using Netcorext.Auth.Enums;

namespace Netcorext.Auth.API.Services.Role.Models;

public class PermissionExtendData
{
    public string Key { get; set; }
    public string Value { get; set; }
    public PermissionType PermissionType { get; set; }
    public bool Allowed { get; set; }
}
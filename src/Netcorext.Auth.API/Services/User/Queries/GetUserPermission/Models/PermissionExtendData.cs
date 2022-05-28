using Netcorext.Auth.Enums;

namespace Netcorext.Auth.API.Services.User.Models;

public class PermissionExtendData
{
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public PermissionType PermissionType { get; set; }
    public bool Allowed { get; set; }
}
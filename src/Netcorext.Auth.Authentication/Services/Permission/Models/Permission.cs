using Netcorext.Auth.Enums;

namespace Netcorext.Auth.Authentication.Services.Permission.Models;

public class Permission
{
    public long Id { get; set; }
    public long RoleId { get; set; }
    public string FunctionId { get; set; } = null!;
    public PermissionType PermissionType { get; set; }
    public bool Allowed { get; set; }
    public int Priority { get; set; }
    public bool ReplaceExtendData { get; set; }
    public DateTimeOffset? ExpireDate { get; set; }
    public IEnumerable<PermissionExtendData> ExtendData { get; set; } = Array.Empty<PermissionExtendData>();
    public bool Disabled { get; set; }
}
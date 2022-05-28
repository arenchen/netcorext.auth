using Netcorext.Auth.Enums;

namespace Netcorext.Auth.API.Services.User.Models;

public class Permission
{
    public string FunctionId { get; set; } = null!;
    public PermissionType PermissionType { get; set; }
    public bool Allowed { get; set; }
    public int Priority { get; set; }
    public bool ReplaceExtendData { get; set; }
    public IEnumerable<PermissionExtendData>? ExtendData { get; set; }
}
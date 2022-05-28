using Netcorext.Auth.Enums;

namespace Netcorext.Auth.API.Services.User.Models;

public class UserPermission
{
    public string FunctionId { get; set; } = null!;
    public PermissionType PermissionType { get; set; }
    public IEnumerable<UserPermissionExtendData>? ExtendData { get; set; }
}
using Netcorext.Auth.Enums;

namespace Netcorext.Auth.Authentication.Services.Permission.Models;

public class SimplePermission
{
    public string FunctionId { get; set; } = null!;
    public PermissionType PermissionType { get; set; }
    public IEnumerable<SimplePermissionExtendData> ExtendData { get; set; } = Array.Empty<SimplePermissionExtendData>();
}
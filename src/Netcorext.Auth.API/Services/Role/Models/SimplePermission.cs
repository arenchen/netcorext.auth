using Netcorext.Auth.Enums;

namespace Netcorext.Auth.API.Services.Role.Models;

public class SimplePermission
{
    public string FunctionId { get; set; } = null!;
    public PermissionType PermissionType { get; set; }
}
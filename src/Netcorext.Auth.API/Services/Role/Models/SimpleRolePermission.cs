using Netcorext.Auth.Enums;

namespace Netcorext.Auth.API.Services.Role.Models;

public class SimpleRolePermission
{
    public long Id { get; set; }
    public string FunctionId { get; set; } = null!;
    public PermissionType PermissionType { get; set; }
}
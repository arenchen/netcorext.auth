namespace Netcorext.Auth.Authentication.Services.Permission.Queries.Models;

public class RolePermission
{
    public long Id { get; set; }
    public long RoleId { get; set; }
    public long PermissionId { get; set; }
}
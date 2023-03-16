namespace Netcorext.Auth.Authentication.Services.Permission.Queries.Models;

public class UserPermission
{
    public IEnumerable<User> Users { get; set; } = null!;
    public IEnumerable<UserPermissionCondition> PermissionConditions { get; set; } = null!;
    public IEnumerable<UserRole> Roles { get; set; } = null!;
}
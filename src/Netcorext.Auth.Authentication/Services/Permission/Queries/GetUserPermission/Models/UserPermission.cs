namespace Netcorext.Auth.Authentication.Services.Permission.Queries.Models;

public class UserPermission
{
    public IEnumerable<UserPermissionCondition> PermissionConditions { get; set; } = null!;
}
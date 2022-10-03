namespace Netcorext.Auth.Authentication.Services.Permission.Queries.Models;

public class RolePermission
{
    public IEnumerable<RolePermissionRule> PermissionRules { get; set; } = null!;
    public IEnumerable<RolePermissionCondition> PermissionConditions { get; set; } = null!;
}
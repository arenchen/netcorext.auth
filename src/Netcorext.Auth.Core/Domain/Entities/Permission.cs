using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace Netcorext.Auth.Domain.Entities;

public class Permission : Entity
{
    public string Name { get; set; } = null!;
    public int Priority { get; set; }
    public bool Disabled { get; set; }
    public string? State { get; set; }
    public virtual ICollection<Rule> Rules { get; set; } = new HashSet<Rule>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new HashSet<RolePermission>();
    public virtual ICollection<RolePermissionCondition> RolePermissionConditions { get; set; } = new HashSet<RolePermissionCondition>();
    public virtual ICollection<UserPermissionCondition> UserPermissionConditions { get; set; } = new HashSet<UserPermissionCondition>();
}
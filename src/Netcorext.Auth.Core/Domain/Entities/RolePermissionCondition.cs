using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace Netcorext.Auth.Domain.Entities;

public class RolePermissionCondition : Entity
{
    public long RoleId { get; set; }
    public long PermissionId { get; set; }
    public int Priority { get; set; }
    public string? Group { get; set; }
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public bool Allowed { get; set; }
    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}
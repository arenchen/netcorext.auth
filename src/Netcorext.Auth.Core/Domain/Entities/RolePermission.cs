using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace Netcorext.Auth.Domain.Entities;

public class RolePermission : Entity
{
    public long PermissionId { get; set; }
    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}
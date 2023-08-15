using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace Netcorext.Auth.Domain.Entities;

public class UserRole : Entity
{
    public long RoleId { get; set; }
    public DateTimeOffset? ExpireDate { get; set; }
    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}
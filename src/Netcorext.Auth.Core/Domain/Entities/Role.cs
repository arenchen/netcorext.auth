using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace Netcorext.Auth.Domain.Entities;

public class Role : Entity
{
    public string Name { get; set; } = null!;
    public int Priority { get; set; }
    public bool Disabled { get; set; }
    public virtual ICollection<RoleExtendData> ExtendData { get; set; } = new HashSet<RoleExtendData>();
    public virtual ICollection<Permission> Permissions { get; set; } = new HashSet<Permission>();
    public virtual ICollection<UserRole> UserRoles { get; set; } = new HashSet<UserRole>();
    public virtual ICollection<ClientRole> ClientRoles { get; set; } = new HashSet<ClientRole>();
}
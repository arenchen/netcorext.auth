using Netcorext.Auth.Enums;
using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace Netcorext.Auth.Domain.Entities;

public class Permission : Entity
{
    public long RoleId { get; set; }
    public string FunctionId { get; set; } = null!;
    public PermissionType PermissionType { get; set; }
    public bool Allowed { get; set; }
    public int Priority { get; set; }
    public bool ReplaceExtendData { get; set; }
    public DateTimeOffset? ExpireDate { get; set; }
    public virtual Role Role { get; set; } = null!;
    public virtual ICollection<PermissionExtendData> ExtendData { get; set; } = new HashSet<PermissionExtendData>();
}
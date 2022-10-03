using Netcorext.Auth.Enums;
using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace Netcorext.Auth.Domain.Entities;

public class Rule : Entity
{
    public long PermissionId { get; set; }
    public string FunctionId { get; set; } = null!;
    public PermissionType PermissionType { get; set; }
    public bool Allowed { get; set; }
    public virtual Permission Permission { get; set; } = null!;
}
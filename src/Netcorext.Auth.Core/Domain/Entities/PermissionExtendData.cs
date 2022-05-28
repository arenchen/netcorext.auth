using Netcorext.Auth.Enums;
using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace Netcorext.Auth.Domain.Entities;

public class PermissionExtendData : Entity
{
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public PermissionType PermissionType { get; set; }
    public bool Allowed { get; set; }
    public virtual Permission Permission { get; set; } = null!;
}
using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace Netcorext.Auth.Domain.Entities;

public class RoleExtendData : Entity
{
    public string Key { get; set; } = null!;
    public string? Value { get; set; }
    public virtual Role Role { get; set; } = null!;
}
using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace Netcorext.Auth.Domain.Entities;

public class UserExtendData : Entity
{
    public string Key { get; set; } = null!;
    public string? Value { get; set; }
    public virtual User User { get; set; } = null!;
}
using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace Netcorext.Auth.Domain.Entities;

public class UserExternalLogin : Entity
{
    public string Provider { get; set; } = null!;
    public string UniqueId { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
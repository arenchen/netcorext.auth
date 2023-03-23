using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace Netcorext.Auth.Domain.Entities;

public class ClientExtendData : Entity
{
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public virtual Client Client { get; set; } = null!;
}
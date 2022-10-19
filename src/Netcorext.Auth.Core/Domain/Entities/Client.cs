using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace Netcorext.Auth.Domain.Entities;

public class Client : Entity
{
    public string Secret { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? CallbackUrl { get; set; }
    public bool AllowedRefreshToken { get; set; }
    public int? TokenExpireSeconds { get; set; }
    public int? RefreshTokenExpireSeconds { get; set; }
    public int? CodeExpireSeconds { get; set; }
    public bool Disabled { get; set; }
    public virtual ICollection<ClientRole> Roles { get; set; } = new HashSet<ClientRole>();
    public virtual ICollection<ClientExtendData> ExtendData { get; set; } = new HashSet<ClientExtendData>();
}
using Netcorext.Auth.Enums;
using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace Netcorext.Auth.Domain.Entities;

public class Token : Entity
{
    public ResourceType ResourceType { get; set; }
    public string ResourceId { get; set; } = null!;
    public string TokenType { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
    public int ExpiresIn { get; set; }
    public long ExpiresAt { get; set; }
    public string? Scope { get; set; }
    public string? RefreshToken { get; set; }
    public int? RefreshExpiresIn { get; set; }
    public long? RefreshExpiresAt { get; set; }
    public TokenRevoke Revoked { get; set; }
}

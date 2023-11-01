using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace Netcorext.Auth.Domain.Entities;

public class BlockedIp : Entity
{
    public string Cidr { get; set; } = null!;
    public long BeginRange { get; set; }
    public long EndRange { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Asn { get; set; }
    public string? Description { get; set; }
}

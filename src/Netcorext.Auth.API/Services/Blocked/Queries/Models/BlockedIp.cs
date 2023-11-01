namespace Netcorext.Auth.API.Services.Blocked.Queries.Models;

public class BlockedIp
{
    public long Id { get; set; }
    public string Cidr { get; set; } = null!;
    public long BeginRange { get; set; }
    public long EndRange { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Asn { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public long CreatorId { get; set; }
    public DateTimeOffset ModificationDate { get; set; }
    public long ModifierId { get; set; }
}

namespace Netcorext.Auth.Authentication.Services.Blocked.Queries.Models;

public class BlockedIp
{
    public long Id { get; set; }
    public string Cidr { get; set; } = null!;
    public long BeginRange { get; set; }
    public long EndRange { get; set; }
}

namespace Netcorext.Auth.Authorization.Models;

public class Role
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public int Priority { get; set; }
    public DateTimeOffset? ExpireDate { get; set; }
}
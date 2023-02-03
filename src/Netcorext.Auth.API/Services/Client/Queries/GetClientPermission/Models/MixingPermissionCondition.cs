namespace Netcorext.Auth.API.Services.Client.Queries.Models;

public class MixingPermissionCondition
{
    public long PermissionId { get; set; }
    public int Priority { get; set; }
    public string? Group { get; set; }
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public bool Allowed { get; set; }
}
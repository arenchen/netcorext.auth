namespace Netcorext.Auth.API.Services.Role.Queries.Models;

public class PermissionCondition
{
    public long PermissionId { get; set; }
    public string? Group { get; set; }
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
}
namespace Netcorext.Auth.Authentication.Services.Permission.Queries.Models;

public class RolePermissionCondition
{
    public long Id { get; set; }
    public long RoleId { get; set; }
    public long PermissionId { get; set; }
    public int Priority { get; set; }
    public string? Group { get; set; }
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public bool Allowed { get; set; }
}
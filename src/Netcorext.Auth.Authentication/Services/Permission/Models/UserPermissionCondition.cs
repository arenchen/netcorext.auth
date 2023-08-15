namespace Netcorext.Auth.Authentication.Services.Permission.Queries.Models;

public class UserPermissionCondition
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long PermissionId { get; set; }
    public string? Group { get; set; }
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public DateTimeOffset? ExpireDate { get; set; }
}
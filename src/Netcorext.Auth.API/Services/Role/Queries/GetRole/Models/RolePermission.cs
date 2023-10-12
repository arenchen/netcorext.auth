namespace Netcorext.Auth.API.Services.Role.Queries.Models;

public class RolePermission
{
    public long PermissionId { get; set; }
    public string Name { get; set; } = null!;
    public string? State { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public long CreatorId { get; set; }
    public DateTimeOffset ModificationDate { get; set; }
    public long ModifierId { get; set; }
}

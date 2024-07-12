namespace Netcorext.Auth.API.Services.Role.Queries.Models;

public class Role
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public int Priority { get; set; }
    public bool Disabled { get; set; }
    public IEnumerable<RoleExtendData>? ExtendData { get; set; }
    public IEnumerable<RolePermission>? Permissions { get; set; }
    public IEnumerable<RolePermissionCondition>? PermissionConditions { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public long CreatorId { get; set; }
    public DateTimeOffset ModificationDate { get; set; }
    public long ModifierId { get; set; }
}

namespace Netcorext.Auth.API.Services.User.Queries.Models;

public class FullUserRole
{
    public long Id { get; set; }
    public long RoleId { get; set; }
    public string Name { get; set; } = null!;
    public int Priority { get; set; }
    public DateTimeOffset? ExpireDate { get; set; }
    public IEnumerable<RoleExtendData>? ExtendData { get; set; }
    public IEnumerable<RolePermission>? Permissions { get; set; }
    public IEnumerable<RolePermissionCondition>? PermissionConditions { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public long CreatorId { get; set; }
    public DateTimeOffset ModificationDate { get; set; }
    public long ModifierId { get; set; }
}

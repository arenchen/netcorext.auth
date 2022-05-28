using Netcorext.Auth.Enums;

namespace Netcorext.Auth.API.Services.Role.Models;

public class Permission
{
    public long Id { get; set; }
    public string FunctionId { get; set; } = null!;
    public PermissionType PermissionType { get; set; }
    public bool Allowed { get; set; }
    public int Priority { get; set; }
    public bool ReplaceExtendData { get; set; }
    public DateTimeOffset? ExpireDate { get; set; }
    public IEnumerable<PermissionExtendData>? ExtendData { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public long CreatorId { get; set; }
    public DateTimeOffset ModificationDate { get; set; }
    public long ModifierId { get; set; }
}
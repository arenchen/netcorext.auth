using Netcorext.Auth.Enums;

namespace Netcorext.Auth.API.Services.Permission.Queries.Models;

public class Rule
{
    public long Id { get; set; }
    public string FunctionId { get; set; } = null!;
    public PermissionType PermissionType { get; set; }
    public bool Allowed { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public long CreatorId { get; set; }
    public DateTimeOffset ModificationDate { get; set; }
    public long ModifierId { get; set; }
}
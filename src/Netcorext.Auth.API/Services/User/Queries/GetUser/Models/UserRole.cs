namespace Netcorext.Auth.API.Services.User.Queries.Models;

public class UserRole
{
    public long RoleId { get; set; }
    public string Name { get; set; } = null!;
    public int Priority { get; set; }
    public DateTimeOffset? ExpireDate { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public long CreatorId { get; set; }
    public DateTimeOffset ModificationDate { get; set; }
    public long ModifierId { get; set; }
}

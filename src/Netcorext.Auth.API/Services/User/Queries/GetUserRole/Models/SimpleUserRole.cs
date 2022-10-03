namespace Netcorext.Auth.API.Services.User.Queries.Models;

public class SimpleUserRole
{
    public long Id { get; set; }
    public long RoleId { get; set; }
    public string Name { get; set; } = null!;
    public DateTimeOffset CreationDate { get; set; }
    public long CreatorId { get; set; }
    public DateTimeOffset ModificationDate { get; set; }
    public long ModifierId { get; set; }
}
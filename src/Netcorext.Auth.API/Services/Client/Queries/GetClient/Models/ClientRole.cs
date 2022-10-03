namespace Netcorext.Auth.API.Services.Client.Queries.Models;

public class ClientRole
{
    public long RoleId { get; set; }
    public string Name { get; set; } = null!;
    public DateTimeOffset? ExpireDate { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public long CreatorId { get; set; }
    public DateTimeOffset ModificationDate { get; set; }
    public long ModifierId { get; set; }
}
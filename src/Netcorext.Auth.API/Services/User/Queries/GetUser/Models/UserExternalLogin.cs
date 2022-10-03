namespace Netcorext.Auth.API.Services.User.Queries.Models;

public class UserExternalLogin
{
    public string? Provider { get; set; }
    public string? UniqueId { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public long CreatorId { get; set; }
    public DateTimeOffset ModificationDate { get; set; }
    public long ModifierId { get; set; }
}
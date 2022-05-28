namespace Netcorext.Auth.API.Services.User.Models;

public class UserExtendData
{
    public string? Key { get; set; }
    public string? Value { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public long CreatorId { get; set; }
    public DateTimeOffset ModificationDate { get; set; }
    public long ModifierId { get; set; }
}
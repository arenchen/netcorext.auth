namespace Netcorext.Auth.API.Services.User.Queries.Models;

public class UserIdentity
{
    public long Id { get; set; }
    public string Username { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
}
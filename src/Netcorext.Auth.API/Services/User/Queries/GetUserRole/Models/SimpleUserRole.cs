namespace Netcorext.Auth.API.Services.User.Models;

public class SimpleUserRole
{
    public long Id { get; set; }
    public long RoleId { get; set; }
    public string Name { get; set; } = null!;
}
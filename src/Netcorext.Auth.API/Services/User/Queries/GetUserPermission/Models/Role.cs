namespace Netcorext.Auth.API.Services.User.Queries.Models;

public class Role
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public IEnumerable<Permission> Permissions { get; set; } = null!;
}
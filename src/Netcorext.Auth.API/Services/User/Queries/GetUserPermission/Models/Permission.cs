namespace Netcorext.Auth.API.Services.User.Queries.Models;

public class Permission
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public int Priority { get; set; }
    public IEnumerable<Rule> Rules { get; set; } = null!;
}
namespace Netcorext.Auth.API.Services.Permission.Queries.Models;

public class Permission
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public int Priority { get; set; }
    public bool Disabled { get; set; }
    public IEnumerable<Rule> Rules { get; set; } = null!;
    public DateTimeOffset CreationDate { get; set; }
    public long CreatorId { get; set; }
    public DateTimeOffset ModificationDate { get; set; }
    public long ModifierId { get; set; }
}
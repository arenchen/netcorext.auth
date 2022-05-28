namespace Netcorext.Auth.Authentication.Services.Route.Models;

public class RouteValue
{
    public string Key { get; set; } = null!;
    public string? Value { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public long CreatorId { get; set; }
    public DateTimeOffset ModificationDate { get; set; }
    public long ModifierId { get; set; }
}
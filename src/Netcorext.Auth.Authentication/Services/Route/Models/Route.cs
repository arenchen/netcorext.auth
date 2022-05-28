using Netcorext.Auth.Enums;

namespace Netcorext.Auth.Authentication.Services.Route.Models;

public class Route
{
    public long Id { get; set; }
    public string Group { get; set; } = null!;
    public string Protocol { get; set; } = null!;
    public string HttpMethod { get; set; } = null!;
    public string BaseUrl { get; set; } = null!;
    public string RelativePath { get; set; } = null!;
    public string Template { get; set; } = null!;
    public string FunctionId { get; set; } = null!;
    public PermissionType NativePermission { get; set; }
    public bool AllowAnonymous { get; set; }
    public string? Tag { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public long CreatorId { get; set; }
    public DateTimeOffset ModificationDate { get; set; }
    public long ModifierId { get; set; }
    public IEnumerable<RouteValue> RouteValues { get; set; } = null!;
}
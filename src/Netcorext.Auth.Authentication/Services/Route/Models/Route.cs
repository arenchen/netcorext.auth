using Netcorext.Auth.Enums;

namespace Netcorext.Auth.Authentication.Services.Route.Models;

public class Route
{
    public string Protocol { get; set; } = null!;
    public string HttpMethod { get; set; } = null!;
    public string RelativePath { get; set; } = null!;
    public string Template { get; set; } = null!;
    public string FunctionId { get; set; } = null!;
    public PermissionType NativePermission { get; set; }
    public bool AllowAnonymous { get; set; }
    public string? Tag { get; set; }
    public IEnumerable<RouteValue> RouteValues { get; set; } = null!;
}
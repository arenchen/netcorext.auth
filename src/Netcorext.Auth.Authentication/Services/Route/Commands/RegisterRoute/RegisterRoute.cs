using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Route;

public class RegisterRoute : IRequest<Result>
{
    public IEnumerable<Route>? Routes { get; set; }

    public class Route
    {
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
        public IEnumerable<RouteValue>? RouteValues { get; set; }
    }

    public class RouteValue
    {
        public string Key { get; set; } = null!;
        public string? Value { get; set; }
    }
}
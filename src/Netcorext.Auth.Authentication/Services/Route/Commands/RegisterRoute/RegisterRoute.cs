using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Route.Commands;

public class RegisterRoute : IRequest<Result>
{
    public RouteGroup[] Groups { get; set; } = null!;

    public class RouteGroup
    {
        public string Name { get; set; } = null!;
        public string BaseUrl { get; set; } = null!;
        public string? ForwarderRequestVersion { get; set; }
        public HttpVersionPolicy? ForwarderHttpVersionPolicy { get; set; }
        public TimeSpan? ForwarderActivityTimeout { get; set; }
        public bool? ForwarderAllowResponseBuffering { get; set; }
        public Route[] Routes { get; set; } = null!;
    }

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
        public RouteValue[]? RouteValues { get; set; }
    }

    public class RouteValue
    {
        public string Key { get; set; } = null!;
        public string? Value { get; set; }
    }
}
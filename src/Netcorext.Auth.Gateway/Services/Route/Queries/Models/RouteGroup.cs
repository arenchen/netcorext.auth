using Yarp.ReverseProxy.Forwarder;

namespace Netcorext.Auth.Gateway.Services.Route.Queries.Models;

public class RouteGroup
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string BaseUrl { get; set; } = null!;
    public ForwarderRequestConfig? ForwarderRequestConfig { get; set; }
    public virtual IEnumerable<Route> Routes { get; set; } = null!;
}

using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace Netcorext.Auth.Domain.Entities;

public class RouteGroup : Entity
{
    public string Name { get; set; } = null!;
    public string BaseUrl { get; set; } = null!;
    public string? ForwarderRequestVersion { get; set; }
    public HttpVersionPolicy? ForwarderHttpVersionPolicy { get; set; }
    public TimeSpan? ForwarderActivityTimeout { get; set; }
    public bool? ForwarderAllowResponseBuffering { get; set; }
    public virtual ICollection<Route> Routes { get; set; } = new HashSet<Route>();
}
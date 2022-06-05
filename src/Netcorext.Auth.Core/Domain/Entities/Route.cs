using Netcorext.Auth.Enums;
using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace Netcorext.Auth.Domain.Entities;

public class Route : Entity
{
    public long GroupId { get; set; }
    public string Protocol { get; set; } = null!;
    public string HttpMethod { get; set; } = null!;
    public string RelativePath { get; set; } = null!;
    public string Template { get; set; } = null!;
    public string FunctionId { get; set; } = null!;
    public PermissionType NativePermission { get; set; }
    public bool AllowAnonymous { get; set; }
    public string? Tag { get; set; }
    public virtual RouteGroup Group { get; set; } = null!;
    public virtual ICollection<RouteValue> RouteValues { get; set; } = new HashSet<RouteValue>();
}
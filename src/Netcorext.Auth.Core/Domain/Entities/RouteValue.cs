using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace Netcorext.Auth.Domain.Entities;

public class RouteValue : Entity
{
    public string Key { get; set; } = null!;
    public string? Value { get; set; }
    public virtual Route Route { get; set; } = null!;
}
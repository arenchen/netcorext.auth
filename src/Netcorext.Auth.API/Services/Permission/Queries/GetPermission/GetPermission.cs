using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Permission.Queries;

public class GetPermission : IRequest<Result<IEnumerable<Models.Permission>>>
{
    public long? Id { get; set; }
    public string? Name { get; set; }
    public bool? Disabled { get; set; }
    public PermissionRule? Rule { get; set; }
    public Paging Paging { get; set; } = null!;

    public class PermissionRule
    {
        public string? FunctionId { get; set; }
        public PermissionType[]? PermissionTypes { get; set; }
        public bool? Allowed { get; set; }
    }
}
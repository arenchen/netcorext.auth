using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Permission.Queries;

public class GetRolePermissionCondition : IRequest<Result<IEnumerable<Models.RolePermissionCondition>>>
{
    public long[]? Ids { get; set; }
}
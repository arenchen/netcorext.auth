using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Permission.Queries;

public class GetUserPermissionCondition : IRequest<Result<IEnumerable<Models.UserPermissionCondition>>>
{
    public long[]? Ids { get; set; }
}
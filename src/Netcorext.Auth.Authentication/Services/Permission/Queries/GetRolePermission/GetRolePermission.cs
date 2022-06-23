using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Permission;

public class GetRolePermission : IRequest<Result<IEnumerable<Models.Permission>>>
{
    public long[]? Ids { get; set; }
}
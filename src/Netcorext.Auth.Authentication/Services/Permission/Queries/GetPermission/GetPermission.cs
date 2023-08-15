using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Permission.Queries;

public class GetPermission : IRequest<Result<IEnumerable<Models.PermissionRule>>>
{
    public long[]? Ids { get; set; }
}
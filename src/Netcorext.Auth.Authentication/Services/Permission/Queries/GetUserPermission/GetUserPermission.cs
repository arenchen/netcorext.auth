using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Permission.Queries;

public class GetUserPermission : IRequest<Result<Models.UserPermission>>
{
    public long[]? Ids { get; set; }
}
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User;

public class GetUserPermission : IRequest<Result<IEnumerable<Models.UserPermission>>>
{
    public long Id { get; set; }
}
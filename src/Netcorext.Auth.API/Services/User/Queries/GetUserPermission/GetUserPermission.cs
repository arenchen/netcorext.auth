using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserPermission : IRequest<Result<IEnumerable<Models.Role>>>
{
    public long Id { get; set; }
}
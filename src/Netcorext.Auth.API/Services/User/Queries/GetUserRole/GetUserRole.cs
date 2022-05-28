using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User;

public class GetUserRole : IRequest<Result<IEnumerable<Models.SimpleUserRole>>>
{
    public long Id { get; set; }
}
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Gateway.Services.Route.Queries;

public class GetRoute : IRequest<Result<IEnumerable<Models.RouteGroup>>>
{
    public long[]? GroupIds { get; set; }
}

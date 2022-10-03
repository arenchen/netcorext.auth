using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Route.Queries;

public class GetRoute : IRequest<Result<IEnumerable<Models.RouteGroup>>>
{
    public long[]? GroupIds { get; set; }
}
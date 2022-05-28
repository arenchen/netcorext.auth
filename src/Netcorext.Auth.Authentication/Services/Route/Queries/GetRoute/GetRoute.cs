using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Route;

public class GetRoute : IRequest<Result<IEnumerable<Models.Route>>>
{
    public long[]? Ids { get; set; }
}
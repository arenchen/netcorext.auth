using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Permission.Queries;

public class GetRouteFunction : IRequest<Result<IEnumerable<string>>> { }
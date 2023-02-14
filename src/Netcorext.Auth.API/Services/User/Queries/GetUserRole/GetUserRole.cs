using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserRole : IRequest<Result<IEnumerable<Models.FullUserRole>>>
{
    public long[] Ids { get; set; } = null!;
}
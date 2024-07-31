using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.User.Queries;

public class GetBlockedUser : IRequest<Result<long[]>>
{
    public long[]? Ids { get; set; }
}

using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Blocked.Queries;

public class GetBlockedIp : IRequest<Result<IEnumerable<Models.BlockedIp>>>
{
    public long[]? Ids { get; set; }
}

using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Client.Queries;

public class GetBlockedClient : IRequest<Result<long[]>>
{
    public long[]? Ids { get; set; }
}

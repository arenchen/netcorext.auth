using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Client.Queries;

public class GetClient : IRequest<Result<Models.Client[]>>
{
    public long[]? Ids { get; set; }
}

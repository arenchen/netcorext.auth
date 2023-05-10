using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserIdentity : IRequest<Result<IEnumerable<Models.UserIdentity>>>
{
    public long[]? Ids { get; set; }
    public string[]? Usernames { get; set; }
}
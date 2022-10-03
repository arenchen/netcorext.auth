using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Queries;

public class ExistsUser : IRequest<Result>
{
    public long? Id { get; set; }
    public string Username { get; set; } = null!;
}
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User;

public class ExistsUser : IRequest<Result>
{
    public string Username { get; set; } = null!;
}
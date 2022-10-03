using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authorization.Services.User.Commands;

public class SignOut : IRequest<Result>
{
    public string Token { get; set; } = null!;
    public bool AllDevice { get; set; }
}
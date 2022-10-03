using Netcorext.Auth.Authorization.Models;
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authorization.Services.User.Commands;

public class SignIn : IRequest<Result<TokenResult>>
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Otp { get; set; }
}
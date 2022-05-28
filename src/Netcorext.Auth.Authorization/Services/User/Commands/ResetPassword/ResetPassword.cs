using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authorization.Services.User;

public class ResetPassword : IRequest<Result>
{
    public long Id { get; set; }
    public string? Password { get; set; }
}
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Token.Queries;

public class ValidateToken : IRequest<Result>
{
    public string Token { get; set; } = null!;
}
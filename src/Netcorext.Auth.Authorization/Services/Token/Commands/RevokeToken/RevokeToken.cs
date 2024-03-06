using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authorization.Services.Token.Commands;

public class RevokeToken : IRequest<Result>
{
    public string? ResourceId { get; set; }
    public string? Token { get; set; }
    public bool? AllDevice { get; set; }
}

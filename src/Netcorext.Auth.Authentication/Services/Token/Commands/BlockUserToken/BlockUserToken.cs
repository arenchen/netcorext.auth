using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Token.Commands;

public class BlockUserToken : IRequest<Result>
{
    public long[] Ids { get; set; } = null!;
}
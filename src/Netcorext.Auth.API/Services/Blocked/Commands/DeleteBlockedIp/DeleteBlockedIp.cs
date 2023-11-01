using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Blocked.Commands;

public class DeleteBlockedIp : IRequest<Result>
{
    public long[] Ids { get; set; } = null!;
}

using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Blocked.Commands;

public class DeleteBlockedIpHandler : IRequestHandler<DeleteBlockedIp, Result>
{
    private readonly DatabaseContext _context;

    public DeleteBlockedIpHandler(DatabaseContextAdapter context)
    {
        _context = context.Master;
    }

    public async Task<Result> Handle(DeleteBlockedIp request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.BlockedIp>();

        var entities = await ds.Where(t => request.Ids.Contains(t.Id)).ToArrayAsync(cancellationToken: cancellationToken);

        ds.RemoveRange(entities);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.SuccessNoContent;
    }
}

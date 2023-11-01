using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Blocked.Queries;

public class GetBlockedIpHandler : IRequestHandler<GetBlockedIp, Result<IEnumerable<Models.BlockedIp>>>
{
    private readonly DatabaseContext _context;

    public GetBlockedIpHandler(DatabaseContextAdapter context)
    {
        _context = context.Slave;
    }

    public async Task<Result<IEnumerable<Models.BlockedIp>>> Handle(GetBlockedIp request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.BlockedIp>();

        Expression<Func<Domain.Entities.BlockedIp, bool>> predicate = p => true;

        if (!request.Ids.IsEmpty())
            predicate = predicate.And(t => request.Ids.Contains(t.Id));

        var entities = await ds.Where(predicate)
                               .Select(t => new Models.BlockedIp
                                            {
                                                Id = t.Id,
                                                Cidr = t.Cidr,
                                                BeginRange = t.BeginRange,
                                                EndRange = t.EndRange,
                                            })
                               .ToArrayAsync(cancellationToken);

        return Result<IEnumerable<Models.BlockedIp>>.Success.Clone(entities);
    }
}

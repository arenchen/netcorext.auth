using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Blocked.Queries;

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

        if (request.Id.HasValue)
            predicate = predicate.And(t => t.Id == request.Id);

        if (!request.Cidr.IsEmpty())
            predicate = predicate.And(t => t.Cidr == request.Cidr);

        if (!request.Country.IsEmpty())
            predicate = predicate.And(t => t.Country == request.Country);

        if (!request.City.IsEmpty())
            predicate = predicate.And(t => t.City == request.City);

        if (!request.Asn.IsEmpty())
            predicate = predicate.And(t => t.Asn == request.Asn);

        var entities = await ds.Where(predicate)
                               .AsNoTracking()
                               .Select(t => new Models.BlockedIp
                                            {
                                                Id = t.Id,
                                                Cidr = t.Cidr,
                                                BeginRange = t.BeginRange,
                                                EndRange = t.EndRange,
                                                Country = t.Country,
                                                City = t.City,
                                                Asn = t.Asn,
                                                Description = t.Description,
                                                CreationDate = t.CreationDate,
                                                CreatorId = t.CreatorId,
                                                ModificationDate = t.ModificationDate,
                                                ModifierId = t.ModifierId
                                            })
                               .ToArrayAsync(cancellationToken);

        return Result<IEnumerable<Models.BlockedIp>>.Success.Clone(entities);
    }
}

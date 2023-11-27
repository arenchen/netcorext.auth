using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Settings;
using Netcorext.Configuration.Extensions;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Blocked.Queries;

public class GetBlockedIpHandler : IRequestHandler<GetBlockedIp, Result<IEnumerable<Models.BlockedIp>>>
{
    private readonly DatabaseContext _context;
    private readonly int _dataSizeLimit;

    public GetBlockedIpHandler(DatabaseContextAdapter context, IOptions<ConfigSettings> config)
    {
        _context = context.Slave;
        _dataSizeLimit = config.Value.Connections.RelationalDb.GetDefault().DataSizeLimit;
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

        var queryEntities = ds.Where(predicate)
                              .OrderBy(t => t.Id)
                              .Take(_dataSizeLimit)
                              .AsNoTracking();

        var pagination = await queryEntities.GroupBy(t => 0)
                                            .Select(t => new
                                                         {
                                                             Count = t.Count(),
                                                             Rows = t.OrderBy(t2 => t2.Id)
                                                                     .Skip(request.Paging.Offset)
                                                                     .Take(request.Paging.Limit)
                                                                     .Select(t2 => new Models.BlockedIp
                                                                                   {
                                                                                       Id = t2.Id,
                                                                                       Cidr = t2.Cidr,
                                                                                       BeginRange = t2.BeginRange,
                                                                                       EndRange = t2.EndRange,
                                                                                       Country = t2.Country,
                                                                                       City = t2.City,
                                                                                       Asn = t2.Asn,
                                                                                       Description = t2.Description,
                                                                                       CreationDate = t2.CreationDate,
                                                                                       CreatorId = t2.CreatorId,
                                                                                       ModificationDate = t2.ModificationDate,
                                                                                       ModifierId = t2.ModifierId
                                                                                   }
                                                                            )
                                                         })
                                            .SingleOrDefaultAsync(cancellationToken);

        request.Paging.Count = pagination?.Count ?? 0;

        var content = pagination?.Rows.ToArray();

        if (content != null && !content.Any()) content = null;

        return Result<IEnumerable<Models.BlockedIp>>.Success.Clone(content, request.Paging);
    }
}

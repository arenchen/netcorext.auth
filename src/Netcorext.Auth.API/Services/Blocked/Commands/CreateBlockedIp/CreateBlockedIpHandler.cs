using Microsoft.EntityFrameworkCore;
using Netcorext.Algorithms;
using Netcorext.Auth.Helpers;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Blocked.Commands;

public class CreateBlockedIpHandler : IRequestHandler<CreateBlockedIp, Result<IEnumerable<long>>>
{
    private readonly DatabaseContext _context;
    private readonly ISnowflake _snowflake;

    public CreateBlockedIpHandler(DatabaseContextAdapter context, ISnowflake snowflake)
    {
        _context = context.Master;
        _snowflake = snowflake;
    }

    public async Task<Result<IEnumerable<long>>> Handle(CreateBlockedIp request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.BlockedIp>();

        var blockedIpList = new List<Domain.Entities.BlockedIp>(request.BlockedIps.Length);

        var lsCidr = request.BlockedIps
                           .Select(t => t.Cidr)
                           .ToArray();

        var conflictCidr = await ds.Where(t => lsCidr.Contains(t.Cidr))
                                   .Select(t => t.Cidr)
                                   .ToArrayAsync(cancellationToken);

        if (conflictCidr.Length > 0)
            return Result<IEnumerable<long>>.Conflict.Clone(string.Join(',', conflictCidr));

        foreach (var blockedIp in request.BlockedIps)
        {
            var (beginRange, endRange, mask) = IpHelper.ParseCidrToRange(blockedIp.Cidr);

            if (beginRange == 0 && endRange == 0 && mask == 0)
                return Result<IEnumerable<long>>.InvalidInput;

            var entity = new Domain.Entities.BlockedIp
                         {
                             Id = _snowflake.Generate(),
                             Cidr = blockedIp.Cidr,
                             BeginRange = beginRange,
                             EndRange = endRange,
                             Country = blockedIp.Country,
                             City = blockedIp.City,
                             Asn = blockedIp.Asn,
                             Description = blockedIp.Description
                         };

            blockedIpList.Add(entity);
        }

        ds.AddRange(blockedIpList);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<IEnumerable<long>>.SuccessCreated.Clone(blockedIpList.Select(t => t.Id).ToArray());
    }
}

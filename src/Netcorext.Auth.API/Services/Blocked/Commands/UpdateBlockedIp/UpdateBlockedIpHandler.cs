using Netcorext.Auth.Helpers;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.EntityFramework.UserIdentityPattern.Extensions;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Blocked.Commands;

public class UpdateBlockedIpHandler : IRequestHandler<UpdateBlockedIp, Result>
{
    private readonly DatabaseContext _context;

    public UpdateBlockedIpHandler(DatabaseContextAdapter context)
    {
        _context = context.Master;
    }

    public async Task<Result> Handle(UpdateBlockedIp request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.BlockedIp>();

        var id = request.Id;

        var entity = await ds.FindAsync(new object[] { id }, cancellationToken);

        if (entity == null) return Result.NotFound;

        var (beginRange, endRange, mask) = IpHelper.ParseCidrToRange(request.Cidr);

        if (beginRange == 0 && endRange == 0 && mask == 0)
            return Result<IEnumerable<long>>.InvalidInput;

        _context.Entry(entity).UpdateProperty(t => t.Cidr, request.Cidr);
        _context.Entry(entity).UpdateProperty(t => t.BeginRange, beginRange);
        _context.Entry(entity).UpdateProperty(t => t.EndRange, endRange);
        _context.Entry(entity).UpdateProperty(t => t.Country, request.Country);
        _context.Entry(entity).UpdateProperty(t => t.City, request.City);
        _context.Entry(entity).UpdateProperty(t => t.Asn, request.Asn);
        _context.Entry(entity).UpdateProperty(t => t.Description, request.Description);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.SuccessNoContent;
    }
}

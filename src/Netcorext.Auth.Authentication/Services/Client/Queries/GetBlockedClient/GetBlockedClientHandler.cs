using System.Linq.Expressions;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Client.Queries;

public class GetBlockedClientHandler : IRequestHandler<GetBlockedClient, Result<long[]>>
{
    private readonly DatabaseContext _context;

    public GetBlockedClientHandler(DatabaseContextAdapter context)
    {
        _context = context.Slave;
    }

    public Task<Result<long[]>> Handle(GetBlockedClient request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Client>();

        Expression<Func<Domain.Entities.Client, bool>> predicate = p => p.Disabled;

        if (!request.Ids.IsEmpty())
            predicate = predicate.And(t => request.Ids.Contains(t.Id));

        var ids = ds.Where(predicate)
                    .Select(t => t.Id)
                    .ToArray();

        return Task.FromResult(Result<long[]>.Success.Clone(ids));
    }
}

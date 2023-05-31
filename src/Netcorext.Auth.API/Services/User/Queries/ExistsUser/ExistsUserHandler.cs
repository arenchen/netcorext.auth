using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Queries;

public class ExistsUserHandler : IRequestHandler<ExistsUser, Result>
{
    private readonly DatabaseContext _context;

    public ExistsUserHandler(DatabaseContextAdapter context)
    {
        _context = context.Slave;
    }

    public async Task<Result> Handle(ExistsUser request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.User>();

        Expression<Func<Domain.Entities.User, bool>> predicate = t => t.NormalizedUsername == request.Username.ToUpper();

        if (request.Id.HasValue)
            predicate = predicate.And(t => t.Id != request.Id.Value);

        if (await ds.AnyAsync(predicate, cancellationToken)) return Result.Success;

        return Result.NotFound;
    }
}
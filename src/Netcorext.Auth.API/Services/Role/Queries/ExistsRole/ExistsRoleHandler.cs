using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role.Queries;

public class ExistsRoleHandler : IRequestHandler<ExistsRole, Result>
{
    private readonly DatabaseContext _context;

    public ExistsRoleHandler(DatabaseContextAdapter context)
    {
        _context = context.Slave;
    }

    public async Task<Result> Handle(ExistsRole request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Role>();

        Expression<Func<Domain.Entities.Role, bool>> predicate = t => t.Name.ToUpper() == request.Name.ToUpper();

        if (request.Id.HasValue)
            predicate = predicate.And(t => t.Id != request.Id.Value);

        if (await ds.AnyAsync(predicate, cancellationToken))
            return Result.Success;

        return Result.NotFound;
    }
}

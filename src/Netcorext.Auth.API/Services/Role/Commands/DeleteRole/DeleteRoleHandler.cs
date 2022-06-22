using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role;

public class DeleteRoleHandler : IRequestHandler<DeleteRole, Result>
{
    private readonly DatabaseContext _context;

    public DeleteRoleHandler(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteRole request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Role>();

        Expression<Func<Domain.Entities.Role, bool>> predicate = p => false;

        predicate = request.Ids.Aggregate(predicate, (current, id) => current.Or(t => t.Id == id));

        var qRole = ds.Where(predicate);

        if (!await qRole.AnyAsync(cancellationToken)) return Result.SuccessNoContent;
        
        ds.RemoveRange(qRole);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.SuccessNoContent;
    }
}
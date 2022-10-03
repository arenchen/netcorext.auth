using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Permission.Commands;

public class DeletePermissionHandler : IRequestHandler<DeletePermission, Result>
{
    private readonly DatabaseContext _context;

    public DeletePermissionHandler(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeletePermission request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Permission>();

        Expression<Func<Domain.Entities.Permission, bool>> predicate = p => false;

        predicate = request.Ids.Aggregate(predicate, (current, id) => current.Or(t => t.Id == id));

        var qPermission = ds.Where(predicate);

        if (!await qPermission.AnyAsync(cancellationToken)) return Result.SuccessNoContent;

        ds.RemoveRange(qPermission);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.SuccessNoContent;
    }
}
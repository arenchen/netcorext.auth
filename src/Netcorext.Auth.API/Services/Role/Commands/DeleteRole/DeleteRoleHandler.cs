using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
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

        var qRole = ds.Where(t => t.Id == request.Id);

        if (!await qRole.AnyAsync(cancellationToken)) return Result.Success;
        
        ds.RemoveRange(qRole);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
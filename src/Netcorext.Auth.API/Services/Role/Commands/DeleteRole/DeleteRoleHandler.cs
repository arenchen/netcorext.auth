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
        var dsExtendData = _context.Set<Domain.Entities.RoleExtendData>();
        var dsPermission = _context.Set<Domain.Entities.Permission>();
        var dsPermissionException = _context.Set<Domain.Entities.PermissionExtendData>();

        var qRole = ds.Where(t => t.Id == request.Id);

        if (!await qRole.AnyAsync(cancellationToken)) return Result.Success;

        var qExtendData = dsExtendData.Where(t => t.Id == request.Id);

        if (await qExtendData.AnyAsync(cancellationToken)) dsExtendData.RemoveRange(qExtendData);

        var qPermissionException = dsPermissionException.Where(t => t.Id == request.Id);
        
        if (await qPermissionException.AnyAsync(cancellationToken)) dsPermissionException.RemoveRange(qPermissionException);
        
        var qPermission = dsPermission.Where(t => t.Id == request.Id);

        if (await qPermission.AnyAsync(cancellationToken)) dsPermission.RemoveRange(qPermission);

        ds.RemoveRange(qRole);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
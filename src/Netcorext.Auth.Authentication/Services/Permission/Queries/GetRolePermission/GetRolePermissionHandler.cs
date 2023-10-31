using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Permission.Queries;

public class GetRolePermissionHandler : IRequestHandler<GetRolePermission, Result<IEnumerable<Models.RolePermission>>>
{
    private readonly DatabaseContext _context;

    public GetRolePermissionHandler(DatabaseContextAdapter context)
    {
        _context = context.Slave;
    }

    public Task<Result<IEnumerable<Models.RolePermission>>> Handle(GetRolePermission request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Role>();

        Expression<Func<Domain.Entities.Role, bool>> predicate = p => !p.Disabled;

        if (!request.Ids.IsEmpty())
            predicate = predicate.And(t => request.Ids.Contains(t.Id));

        var qRole = ds.Where(predicate)
                      .AsNoTracking();

        var result = qRole.SelectMany(t => t.Permissions
                                            .Where(t2 => !t2.Permission.Disabled)
                                            .Select(t2 => new Models.RolePermission
                                                          {
                                                              Id = t2.Id,
                                                              RoleId = t.Id,
                                                              PermissionId = t2.PermissionId
                                                          }))
                          .ToArray();

        return Task.FromResult(Result<IEnumerable<Models.RolePermission>>.Success.Clone(result));
    }
}

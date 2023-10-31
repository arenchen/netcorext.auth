using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Permission.Queries;

public class GetRolePermissionConditionHandler : IRequestHandler<GetRolePermissionCondition, Result<IEnumerable<Models.RolePermissionCondition>>>
{
    private readonly DatabaseContext _context;

    public GetRolePermissionConditionHandler(DatabaseContextAdapter context)
    {
        _context = context.Slave;
    }

    public Task<Result<IEnumerable<Models.RolePermissionCondition>>> Handle(GetRolePermissionCondition request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Role>();

        Expression<Func<Domain.Entities.Role, bool>> predicate = p => !p.Disabled;

        if (!request.Ids.IsEmpty())
            predicate = predicate.And(t => request.Ids.Contains(t.Id));

        var qRole = ds.Where(predicate)
                      .AsNoTracking();

        var result = qRole.SelectMany(t => t.PermissionConditions
                                            .Where(t2 => !t2.Permission.Disabled)
                                            .Select(t2 => new Models.RolePermissionCondition
                                                          {
                                                              Id = t2.Id,
                                                              RoleId = t2.RoleId,
                                                              PermissionId = t2.PermissionId,
                                                              Group = t2.Group,
                                                              Key = t2.Key,
                                                              Value = t2.Value
                                                          }))
                          .ToArray();

        return Task.FromResult(Result<IEnumerable<Models.RolePermissionCondition>>.Success.Clone(result));
    }
}

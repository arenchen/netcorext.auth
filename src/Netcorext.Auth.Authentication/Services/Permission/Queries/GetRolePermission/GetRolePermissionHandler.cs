using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Permission.Queries;

public class GetRolePermissionHandler : IRequestHandler<GetRolePermission, Result<Models.RolePermission>>
{
    private readonly DatabaseContext _context;

    public GetRolePermissionHandler(DatabaseContextAdapter context)
    {
        _context = context.Slave;
    }

    public Task<Result<Models.RolePermission>> Handle(GetRolePermission request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Role>();

        Expression<Func<Domain.Entities.Role, bool>> predicate = p => !p.Disabled;

        if (!request.Ids.IsEmpty())
            predicate = predicate.And(t => request.Ids.Contains(t.Id));

        var qRole = ds.Where(predicate)
                      .AsNoTracking();

        var conditions = qRole.SelectMany(t => t.PermissionConditions
                                                .Where(t2 => !t2.Permission.Disabled)
                                                .Select(t2 => new Models.RolePermissionCondition
                                                              {
                                                                  Id = t2.Id,
                                                                  RoleId = t2.RoleId,
                                                                  PermissionId = t2.PermissionId,
                                                                  Priority = t2.Priority,
                                                                  Group = t2.Group,
                                                                  Key = t2.Key,
                                                                  Value = t2.Value,
                                                                  Allowed = t2.Allowed
                                                              }));

        var rules = qRole.SelectMany(t => t.Permissions
                                           .Where(t2 => !t2.Permission.Disabled)
                                           .SelectMany(t2 => t2.Permission
                                                               .Rules
                                                               .Select(t3 => new Models.RolePermissionRule
                                                                             {
                                                                                 Id = t3.Id,
                                                                                 RoleId = t.Id,
                                                                                 PermissionId = t3.PermissionId,
                                                                                 FunctionId = t3.FunctionId,
                                                                                 Priority = t2.Permission.Priority,
                                                                                 PermissionType = t3.PermissionType,
                                                                                 Allowed = t3.Allowed
                                                                             })));

        var result = new Models.RolePermission
                     {
                         PermissionConditions = conditions.ToArray(),
                         PermissionRules = rules.ToArray()
                     };

        return Task.FromResult(Result<Models.RolePermission>.Success.Clone(result));
    }
}
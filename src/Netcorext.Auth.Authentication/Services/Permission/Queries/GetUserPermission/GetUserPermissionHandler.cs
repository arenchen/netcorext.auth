using System.Linq.Expressions;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Permission.Queries;

public class GetUserPermissionHandler : IRequestHandler<GetUserPermission, Result<Models.UserPermission>>
{
    private readonly DatabaseContext _context;

    public GetUserPermissionHandler(DatabaseContext context)
    {
        _context = context;
    }

    public Task<Result<Models.UserPermission>> Handle(GetUserPermission request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.User>();
        var dsCondition = _context.Set<Domain.Entities.UserPermissionCondition>();
        var dsRole = _context.Set<Domain.Entities.UserRole>();

        Expression<Func<Domain.Entities.User, bool>> predicate = p => !p.Disabled;
        Expression<Func<Domain.Entities.UserPermissionCondition, bool>> predicateCondition = p => (p.ExpireDate == null || p.ExpireDate > DateTime.UtcNow) && !p.User.Disabled;
        Expression<Func<Domain.Entities.UserRole, bool>> predicateRole = p => (p.ExpireDate == null || p.ExpireDate > DateTime.UtcNow) && !p.Role.Disabled && !p.User.Disabled;

        if (!request.Ids.IsEmpty())
        {
            predicate = predicate.And(t => request.Ids.Contains(t.Id));
            predicateCondition = predicateCondition.And(t => request.Ids.Contains(t.Id));
            predicateRole = predicateRole.And(t => request.Ids.Contains(t.Id));
        }

        var user = ds.Where(predicate)
                     .Select(t => new Models.User
                                  {
                                      Id = t.Id
                                  });

        var condition = dsCondition.Where(predicateCondition)
                                   .Select(t => new Models.UserPermissionCondition
                                                {
                                                    Id = t.Id,
                                                    UserId = t.UserId,
                                                    PermissionId = t.PermissionId,
                                                    Priority = t.Priority,
                                                    Group = t.Group,
                                                    Key = t.Key,
                                                    Value = t.Value,
                                                    Allowed = t.Allowed,
                                                    ExpireDate = t.ExpireDate
                                                });

        var role = dsRole.Where(predicateRole)
                         .Select(t => new Models.UserRole
                                      {
                                          Id = t.Id,
                                          RoleId = t.RoleId,
                                          ExpireDate = t.ExpireDate
                                      });

        var result = new Models.UserPermission
                     {
                         Users = user.ToArray(),
                         PermissionConditions = condition.ToArray(),
                         Roles = role.ToArray()
                     };

        return Task.FromResult(Result<Models.UserPermission>.Success.Clone(result));
    }
}
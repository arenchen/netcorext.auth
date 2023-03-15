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
        var ds = _context.Set<Domain.Entities.UserPermissionCondition>();

        Expression<Func<Domain.Entities.UserPermissionCondition, bool>> predicate = p => (p.ExpireDate == null || p.ExpireDate > DateTime.UtcNow) && !p.User.Disabled;

        if (!request.Ids.IsEmpty())
            predicate = predicate.And(t => request.Ids.Contains(t.Id));

        var condition = ds.Where(predicate)
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

        var result = new Models.UserPermission
                     {
                         PermissionConditions = condition.ToArray()
                     };

        return Task.FromResult(Result<Models.UserPermission>.Success.Clone(result));
    }
}
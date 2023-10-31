using System.Linq.Expressions;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Permission.Queries;

public class GetUserPermissionConditionHandler : IRequestHandler<GetUserPermissionCondition, Result<IEnumerable<Models.UserPermissionCondition>>>
{
    private readonly DatabaseContext _context;

    public GetUserPermissionConditionHandler(DatabaseContextAdapter context)
    {
        _context = context.Slave;
    }

    public Task<Result<IEnumerable<Models.UserPermissionCondition>>> Handle(GetUserPermissionCondition request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.UserPermissionCondition>();

        Expression<Func<Domain.Entities.UserPermissionCondition, bool>> predicate = p => (p.ExpireDate == null || p.ExpireDate > DateTimeOffset.UtcNow) && !p.User.Disabled;

        if (!request.Ids.IsEmpty())
            predicate = predicate.And(t => request.Ids.Contains(t.UserId));

        var result = ds.Where(predicate)
                       .Select(t => new Models.UserPermissionCondition
                                    {
                                        Id = t.Id,
                                        UserId = t.UserId,
                                        PermissionId = t.PermissionId,
                                        Group = t.Group,
                                        Key = t.Key,
                                        Value = t.Value,
                                        ExpireDate = t.ExpireDate
                                    })
                       .ToArray();

        return Task.FromResult(Result<IEnumerable<Models.UserPermissionCondition>>.Success.Clone(result));
    }
}

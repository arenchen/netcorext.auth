using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Permission.Queries;

public class GetPermissionHandler : IRequestHandler<GetPermission, Result<IEnumerable<Models.PermissionRule>>>
{
    private readonly DatabaseContext _context;

    public GetPermissionHandler(DatabaseContextAdapter context)
    {
        _context = context.Slave;
    }

    public Task<Result<IEnumerable<Models.PermissionRule>>> Handle(GetPermission request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Permission>();

        Expression<Func<Domain.Entities.Permission, bool>> predicate = p => !p.Disabled;

        if (!request.Ids.IsEmpty())
            predicate = predicate.And(t => request.Ids.Contains(t.Id));

        var qPermission = ds.Where(predicate)
                            .AsNoTracking();

        var result = qPermission.SelectMany(t => t.Rules.Select(t2 => new Models.PermissionRule
                                                                      {
                                                                          PermissionId = t.Id,
                                                                          Id = t2.Id,
                                                                          FunctionId = t2.FunctionId.ToUpper(),
                                                                          Priority = t.Priority,
                                                                          PermissionType = t2.PermissionType,
                                                                          Allowed = t2.Allowed
                                                                      }));

        return Task.FromResult(Result<IEnumerable<Models.PermissionRule>>.Success.Clone(result));
    }
}
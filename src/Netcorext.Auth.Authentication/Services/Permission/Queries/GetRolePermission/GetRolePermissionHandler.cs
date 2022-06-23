using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Permission;

public class GetRolePermissionHandler : IRequestHandler<GetRolePermission, Result<IEnumerable<Models.Permission>>>
{
    private readonly DatabaseContext _context;

    public GetRolePermissionHandler(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Result<IEnumerable<Models.Permission>>> Handle(GetRolePermission request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Role>();

        Expression<Func<Domain.Entities.Role, bool>> predicate = p => request.Ids == null;

        if (request.Ids != null && request.Ids.Any())
        {
            predicate = request.Ids.Aggregate(predicate, (current, id) => current.Or(p => p.Id == id));
        }

        var qRole = ds.Include(t => t.ExtendData)
                      .Include(t => t.Permissions).ThenInclude(t => t.ExtendData)
                      .Where(predicate)
                      .AsNoTracking();

        var content = qRole.SelectMany(t => t.Permissions)
                           .Where(t => t.ExpireDate == null || t.ExpireDate < DateTimeOffset.UtcNow)
                           .Select(t => new Models.Permission
                                        {
                                            Id = t.Id,
                                            RoleId = t.RoleId,
                                            FunctionId = t.FunctionId,
                                            PermissionType = t.PermissionType,
                                            Allowed = t.Allowed,
                                            Priority = t.Priority,
                                            ReplaceExtendData = t.ReplaceExtendData,
                                            ExpireDate = t.ExpireDate,
                                            ExtendData = t.ExtendData
                                                          .Select(t2 => new Models.PermissionExtendData
                                                                        {
                                                                            Key = t2.Key,
                                                                            Value = t2.Value,
                                                                            PermissionType = t2.PermissionType,
                                                                            Allowed = t2.Allowed
                                                                        }),
                                            Disabled = t.Role.Disabled
                                        });

        if (!await content.AnyAsync(cancellationToken)) content = null;

        return Result<IEnumerable<Models.Permission>>.Success.Clone(content?.ToArray());
    }
}
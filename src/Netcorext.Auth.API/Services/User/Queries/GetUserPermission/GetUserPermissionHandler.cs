using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Settings;
using Netcorext.Configuration.Extensions;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserPermissionHandler : IRequestHandler<GetUserPermission, Result<IEnumerable<Models.Role>>>
{
    private readonly DatabaseContext _context;
    private readonly int _dataSizeLimit;

    public GetUserPermissionHandler(DatabaseContext context, IOptions<ConfigSettings> config)
    {
        _context = context;
        _dataSizeLimit = config.Value.Connections.RelationalDb.GetDefault().DataSizeLimit;
    }

    public Task<Result<IEnumerable<Models.Role>>> Handle(GetUserPermission request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Role>();
        var dsUserRole = _context.Set<Domain.Entities.UserRole>();

        var roleIds = dsUserRole.Where(t => t.Id == request.Id && !t.Role.Disabled && (t.ExpireDate == null || t.ExpireDate > DateTimeOffset.UtcNow))
                                .Select(t => t.RoleId)
                                .ToArray();

        Expression<Func<Domain.Entities.Role, bool>> predicate = p => roleIds.Contains(p.Id);

        var qRole = ds.Where(predicate)
                      .OrderBy(t => t.Id)
                      .Take(_dataSizeLimit)
                      .AsNoTracking();

        var result = qRole.Select(t => new Models.Role
                                       {
                                           Id = t.Id,
                                           Name = t.Name,
                                           Permissions = t.Permissions
                                                          .Where(t2 => !t2.Permission.Disabled)
                                                          .Select(t2 => new Models.Permission
                                                                        {
                                                                            Id = t2.PermissionId,
                                                                            Name = t2.Permission.Name,
                                                                            Priority = t2.Permission.Priority,
                                                                            Rules = t2.Permission.Rules.Select(t3 => new Models.Rule
                                                                                                                     {
                                                                                                                         Id = t3.Id,
                                                                                                                         FunctionId = t3.FunctionId,
                                                                                                                         PermissionType = t3.PermissionType,
                                                                                                                         Allowed = t3.Allowed
                                                                                                                     })
                                                                        })
                                       });

        return Task.FromResult(Result<IEnumerable<Models.Role>>.Success.Clone(result));
    }
}
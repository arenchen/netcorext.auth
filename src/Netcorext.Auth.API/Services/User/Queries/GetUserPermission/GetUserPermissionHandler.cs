using Netcorext.Auth.Domain.Entities;
using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserPermissionHandler : IRequestHandler<GetUserPermission, Result<IEnumerable<long>>>
{
    private readonly DatabaseContext _context;

    public GetUserPermissionHandler(DatabaseContext context)
    {
        _context = context;
    }

    public Task<Result<IEnumerable<long>>> Handle(GetUserPermission request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<UserRole>();

        var roles = ds.Where(t => t.Id == request.Id && !t.Role.Disabled)
                      .Select(t => t.Role);

        var roleIds = roles.Select(t => t.Id)
                           .ToArray();

        if (!roleIds.Any()) return Task.FromResult(Result<IEnumerable<long>>.Success);

        var rolePermissionRules = roles.SelectMany(t => t.Permissions
                                                         .Where(t2 => !t2.Permission.Disabled)
                                                         .SelectMany(t2 => t2.Permission.Rules
                                                                             .Select(t3 => new
                                                                                           {
                                                                                               t3.Id,
                                                                                               RoleId = t.Id,
                                                                                               t3.PermissionId,
                                                                                               t3.FunctionId,
                                                                                               t2.Permission.Priority,
                                                                                               t3.PermissionType,
                                                                                               t3.Allowed
                                                                                           })))
                                       .ToArray();

        if (!rolePermissionRules.Any()) return Task.FromResult(Result<IEnumerable<long>>.Success);

        var functions = rolePermissionRules.GroupBy(t => new { t.FunctionId, t.Priority }, t => new { t.PermissionType, t.Allowed })
                                           .Select(t =>
                                                   {
                                                       // 先將同權重的權限最大化
                                                       var p = t.Aggregate((c, n) =>
                                                                           {
                                                                               var pt = c.Allowed ? c.PermissionType : PermissionType.None;

                                                                               pt = n.Allowed ? pt | n.PermissionType : pt;

                                                                               return new
                                                                                      {
                                                                                          PermissionType = pt,
                                                                                          Allowed = c.Allowed | n.Allowed
                                                                                      };
                                                                           });

                                                       return new
                                                              {
                                                                  t.Key.FunctionId,
                                                                  t.Key.Priority,
                                                                  p.PermissionType,
                                                                  p.Allowed
                                                              };
                                                   })
                                           .OrderBy(t => t.FunctionId).ThenBy(t => t.Priority)
                                           .GroupBy(t => t.FunctionId,
                                                    t => new { t.PermissionType, t.Allowed })
                                           .Select(t =>
                                                   {
                                                       // 最終以優先度高的權限為主
                                                       var p = t.Aggregate((c, n) =>
                                                                           {
                                                                               var pt = c.Allowed ? c.PermissionType : PermissionType.None;

                                                                               pt = n.Allowed ? pt | n.PermissionType : (pt ^ n.PermissionType) & pt;

                                                                               return new
                                                                                      {
                                                                                          PermissionType = pt,
                                                                                          Allowed = pt != PermissionType.None
                                                                                      };
                                                                           });

                                                       return new
                                                              {
                                                                  FunctionId = t.Key,
                                                                  p.PermissionType
                                                              };
                                                   });

        var result = rolePermissionRules.Join(functions, r => r.FunctionId, f => f.FunctionId,
                                              (r, f) => new
                                                        {
                                                            r.PermissionId,
                                                            PermissionType = r.PermissionType & f.PermissionType
                                                        })
                                        .Where(t => t.PermissionType != PermissionType.None)
                                        .Select(t => t.PermissionId);

        return Task.FromResult(Result<IEnumerable<long>>.Success.Clone(result));
    }
}
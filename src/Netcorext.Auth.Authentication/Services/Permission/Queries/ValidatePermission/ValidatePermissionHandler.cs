using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Memory;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Permission;

public class ValidatePermissionHandler : IRequestHandler<ValidatePermission, Result>
{
    private readonly DatabaseContext _context;
    private readonly IMemoryCache _cache;

    public ValidatePermissionHandler(DatabaseContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public Task<Result> Handle(ValidatePermission request, CancellationToken cancellationToken = default)
    {
        var roleIds = Array.Empty<long>();

        if (request.UserId.HasValue)
        {
            var ds = _context.Set<Domain.Entities.UserRole>();
            var userRoles = ds.Where(t => t.Id == request.UserId).Select(t => t.RoleId);
        
            roleIds = userRoles.ToArray();
        }

        if (request.RoleId != null && request.RoleId.Any())
        {
            roleIds = roleIds.Union(request.RoleId).ToArray();
        }

        if (!roleIds.Any()) return Task.FromResult(Result.Forbidden);

        var cachePermissions = (_cache.Get<Dictionary<long, Models.Permission>>(ConfigSettings.CACHE_ROLE_PERMISSION) ?? new Dictionary<long, Models.Permission>()).Values.Where(t => !t.Disabled).ToArray();

        if (!cachePermissions.Any()) return Task.FromResult(Result.Forbidden);

        var permissions = cachePermissions.Where(t => t.FunctionId == request.FunctionId)
                                          .In(t => t.RoleId, roleIds)
                                          .ToArray();

        if (!permissions.Any()) return Task.FromResult(Result.Forbidden);

        var maxPermissions = permissions.GroupBy(t => new { t.FunctionId, t.Priority },
                                                 t => new { t.PermissionType, t.Allowed, t.ReplaceExtendData, t.ExtendData })
                                        .Select(t =>
                                                {
                                                    // 先將同權重的權限最大化
                                                    var p = t.Aggregate((c, n) =>
                                                                        {
                                                                            var pt = c.Allowed ? c.PermissionType : PermissionType.None;

                                                                            pt = n.Allowed ? pt | n.PermissionType : pt;

                                                                            var replaceExtendData = c.ReplaceExtendData | n.ReplaceExtendData;

                                                                            return new
                                                                                   {
                                                                                       PermissionType = pt,
                                                                                       Allowed = c.Allowed | n.Allowed,
                                                                                       ReplaceExtendData = replaceExtendData,
                                                                                       ExtendData = replaceExtendData ? n.ExtendData : c.ExtendData.Union(n.ExtendData)
                                                                                   };
                                                                        });

                                                    return new
                                                           {
                                                               t.Key.FunctionId,
                                                               t.Key.Priority,
                                                               p.PermissionType,
                                                               p.Allowed,
                                                               p.ReplaceExtendData,
                                                               ExtendData = p.ExtendData
                                                                             .GroupBy(ed => new { ed.Key, ed.Value }, data => new { data.PermissionType, data.Allowed })
                                                                             .Select(t2 =>
                                                                                     {
                                                                                         var p = t2.Aggregate((c, n) =>
                                                                                                              {
                                                                                                                  var pt = c.Allowed ? c.PermissionType : PermissionType.None;

                                                                                                                  pt = n.Allowed ? pt | n.PermissionType : pt;

                                                                                                                  return new
                                                                                                                         {
                                                                                                                             PermissionType = pt,
                                                                                                                             Allowed = c.Allowed | n.Allowed
                                                                                                                         };
                                                                                                              });

                                                                                         return new Models.PermissionExtendData
                                                                                                {
                                                                                                    Key = t2.Key.Key,
                                                                                                    Value = t2.Key.Value,
                                                                                                    PermissionType = p.PermissionType,
                                                                                                    Allowed = p.Allowed
                                                                                                };
                                                                                     })
                                                           };
                                                })
                                        .OrderBy(t => t.FunctionId).ThenBy(t => t.Priority)
                                        .GroupBy(t => t.FunctionId,
                                                 t => new { t.PermissionType, t.Allowed, t.ReplaceExtendData, t.ExtendData })
                                        .Select(t =>
                                                {
                                                    // 最終以優先度高的權限為主
                                                    var p = t.Aggregate((c, n) =>
                                                                        {
                                                                            var pt = c.Allowed ? c.PermissionType : PermissionType.None;

                                                                            pt = n.Allowed ? pt | n.PermissionType : (pt ^ n.PermissionType) & pt;

                                                                            var replaceExtendData = c.ReplaceExtendData | n.ReplaceExtendData;

                                                                            return new
                                                                                   {
                                                                                       PermissionType = pt,
                                                                                       Allowed = pt != PermissionType.None,
                                                                                       ReplaceExtendData = c.ReplaceExtendData | n.ReplaceExtendData,
                                                                                       ExtendData = replaceExtendData ? n.ExtendData : c.ExtendData.Union(n.ExtendData)
                                                                                   };
                                                                        });

                                                    return new Models.SimplePermission
                                                           {
                                                               FunctionId = t.Key,
                                                               PermissionType = p.PermissionType,
                                                               ExtendData = p.ExtendData
                                                                             .GroupBy(ed => new { ed.Key, ed.Value }, data => new { data.PermissionType, data.Allowed })
                                                                             .Select(t2 =>
                                                                                     {
                                                                                         var pt = t2.Aggregate((c, n) =>
                                                                                                               {
                                                                                                                   var pt = c.Allowed ? c.PermissionType : PermissionType.None;

                                                                                                                   pt = n.Allowed ? pt | n.PermissionType : pt ^ n.PermissionType;

                                                                                                                   return new
                                                                                                                          {
                                                                                                                              PermissionType = pt,
                                                                                                                              Allowed = pt != PermissionType.None
                                                                                                                          };
                                                                                                               });

                                                                                         return new Models.SimplePermissionExtendData
                                                                                                {
                                                                                                    Key = t2.Key.Key,
                                                                                                    Value = t2.Key.Value,
                                                                                                    PermissionType = pt.Allowed ? pt.PermissionType : PermissionType.None
                                                                                                };
                                                                                     })
                                                                             .ToArray()
                                                           };
                                                })
                                        .ToArray();

        var permission = maxPermissions.FirstOrDefault();

        if (permission == null) return Task.FromResult(Result.Forbidden);

        if (request.ExtendData == null || !permission.ExtendData.Any())
        {
            return Task.FromResult((permission.PermissionType & request.PermissionType) != PermissionType.None ? Result.Success : Result.Forbidden);
        }

        Expression<Func<Models.SimplePermissionExtendData, bool>> predicate = p => true;

        predicate = request.ExtendData.Aggregate(predicate, (expression, data) => expression.And(t => t.Key == data.Key && t.Value == data.Value));

        var extendData = permission.ExtendData.FirstOrDefault(predicate.Compile());

        if (extendData == null) return Task.FromResult(Result.Success);

        var result = (extendData.PermissionType & request.PermissionType) != PermissionType.None ? Result.Success : Result.Forbidden;

        return Task.FromResult(result);
    }
}
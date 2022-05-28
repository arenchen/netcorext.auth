using Microsoft.EntityFrameworkCore;
using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User;

public class GetUserPermissionHandler : IRequestHandler<GetUserPermission, Result<IEnumerable<Models.UserPermission>>>
{
    private readonly DatabaseContext _context;

    public GetUserPermissionHandler(DatabaseContext context)
    {
        _context = context;
    }

    public Task<Result<IEnumerable<Models.UserPermission>>> Handle(GetUserPermission request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.UserRole>();

        var qPermission = ds.Where(t => t.Id == request.Id && (t.ExpireDate == null || t.ExpireDate > DateTimeOffset.UtcNow))
                            .Include(t => t.Role).ThenInclude(t => t.Permissions).ThenInclude(t => t.ExtendData)
                            .SelectMany(t => t.Role.Permissions)
                            .AsNoTracking();

        var result = qPermission.Select(t => new Models.Permission
                                             {
                                                 FunctionId = t.FunctionId,
                                                 PermissionType = t.PermissionType,
                                                 Allowed = t.Allowed,
                                                 Priority = t.Priority,
                                                 ReplaceExtendData = t.ReplaceExtendData,
                                                 ExtendData = t.ExtendData.Select(t2 => new Models.PermissionExtendData
                                                                                        {
                                                                                            Key = t2.Key,
                                                                                            Value = t2.Value,
                                                                                            PermissionType = t2.PermissionType,
                                                                                            Allowed = t2.Allowed
                                                                                        })
                                             })
                                .AsEnumerable()
                                .GroupBy(t => new { t.FunctionId, t.Priority },
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

                                            return new Models.UserPermission
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

                                                                                 return new Models.UserPermissionExtendData
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
        
        return Task.FromResult(Result<IEnumerable<Models.UserPermission>>.Success.Clone(result));
    }
}
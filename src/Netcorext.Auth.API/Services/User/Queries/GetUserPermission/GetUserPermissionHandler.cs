using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserPermissionHandler : IRequestHandler<GetUserPermission, Result<IEnumerable<Models.UserPermission>>>
{
    private readonly DatabaseContext _context;

    public GetUserPermissionHandler(DatabaseContextAdapter context)
    {
        _context = context.Slave;
    }

    public async Task<Result<IEnumerable<Models.UserPermission>>> Handle(GetUserPermission request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.User>();
        var dsRole = _context.Set<Domain.Entities.Role>();

        var groups = request.PermissionConditions?
                            .Where(t => !t.Group.IsEmpty())
                            .Select(t => t.Group!)
                            .Distinct()
                            .ToArray() ?? Array.Empty<string>();

        var user = await ds
                        .AsNoTracking()
                        .Where(t => t.Id == request.Id)
                        .Select(t => new
                                     {
                                         t.Disabled,
                                         Roles = t.Roles
                                                  .Where(t2 => t2.ExpireDate > DateTimeOffset.UtcNow && !t2.Role.Disabled)
                                                  .Select(t2 => t2.RoleId)
                                                  .ToArray(),
                                         PermissionConditions = t.PermissionConditions
                                                                 .Where(t2 => t2.ExpireDate > DateTimeOffset.UtcNow && (t2.Group == null || groups.Contains(t2.Group)))
                                                                 .Select(t2 => new Models.MixingPermissionCondition
                                                                               {
                                                                                   PermissionId = t2.PermissionId,
                                                                                   Priority = t2.Priority,
                                                                                   Group = t2.Group,
                                                                                   Key = t2.Key,
                                                                                   Value = t2.Value,
                                                                                   Allowed = t2.Allowed
                                                                               })
                                                                 .ToArray()
                                     })
                        .FirstOrDefaultAsync(cancellationToken);

        if (user == null) return Result<IEnumerable<Models.UserPermission>>.NotFound;

        var emptyContent = Array.Empty<Models.UserPermission>();

        var content = new List<Models.UserPermission>();

        if (user.Disabled) return Result<IEnumerable<Models.UserPermission>>.Success.Clone(emptyContent);

        var roleIds = user.Roles;

        var roles = dsRole
                   .AsSplitQuery()
                   .AsNoTracking()
                   .Where(t => roleIds.Contains(t.Id) && !t.Disabled)
                   .Select(t => new
                                {
                                    RolePermissions = t.Permissions
                                                       .Select(t2 => t2.Permission)
                                                       .Where(t2 => !t2.Disabled)
                                                       .SelectMany(t2 => t2.Rules.Select(t3 => new Models.MixingPermissionRule
                                                                                               {
                                                                                                   Id = t3.Id,
                                                                                                   PermissionId = t3.PermissionId,
                                                                                                   FunctionId = t3.FunctionId,
                                                                                                   Priority = t2.Priority,
                                                                                                   PermissionType = t3.PermissionType,
                                                                                                   Allowed = t3.Allowed
                                                                                               }))
                                                       .ToArray(),
                                    RolePermissionConditions = t.PermissionConditions
                                                                .Where(t2 => t2.Group == null || groups.Contains(t2.Group))
                                                                .Select(t2 => new Models.MixingPermissionCondition
                                                                              {
                                                                                  PermissionId = t2.PermissionId,
                                                                                  Priority = t2.Priority,
                                                                                  Group = t2.Group,
                                                                                  Key = t2.Key,
                                                                                  Value = t2.Value,
                                                                                  Allowed = t2.Allowed
                                                                              })
                                                                .ToArray()
                                })
                   .ToArray();

        if (!roles.Any())
            return Result<IEnumerable<Models.UserPermission>>.Success.Clone(emptyContent);

        var rolePermissions = roles.SelectMany(t => t.RolePermissions)
                                   .ToArray();

        if (!rolePermissions.Any()) return Result<IEnumerable<Models.UserPermission>>.Success.Clone(emptyContent);

        if (request.PermissionConditions == null || !request.PermissionConditions.Any())
        {
            content.Add(await GetPermissionsWithoutConditionAsync(rolePermissions));

            return Result<IEnumerable<Models.UserPermission>>.Success.Clone(content);
        }

        var rolePermissionConditions = roles.SelectMany(t => t.RolePermissionConditions)
                                            .ToArray();

        var userPermissionConditions = user.PermissionConditions;

        var conditions = rolePermissionConditions.Union(userPermissionConditions)
                                                 .Distinct()
                                                 .ToArray();

        foreach (var permissionCondition in request.PermissionConditions)
        {
            content.Add(await GetPermissionsAsync(permissionCondition, rolePermissions, conditions));
        }

        return Result<IEnumerable<Models.UserPermission>>.Success.Clone(content);
    }

    private Task<Models.UserPermission> GetPermissionsWithoutConditionAsync(IEnumerable<Models.MixingPermissionRule> permissions)
    {
        var permissionRules = permissions.Select(t => new
                                                      {
                                                          t.Id,
                                                          t.PermissionId,
                                                          t.FunctionId,
                                                          t.Priority,
                                                          t.PermissionType,
                                                          t.Allowed
                                                      })
                                         .ToArray();

        if (!permissionRules.Any()) return Task.FromResult(new Models.UserPermission());

        var functions = permissionRules.GroupBy(t => new { t.FunctionId, t.Priority }, t => new { t.PermissionType, t.Allowed })
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

        var ids = permissionRules.Join(functions, r => r.FunctionId, f => f.FunctionId,
                                       (r, f) => new
                                                 {
                                                     r.PermissionId,
                                                     PermissionType = r.PermissionType & f.PermissionType
                                                 })
                                 .Where(t => t.PermissionType != PermissionType.None)
                                 .Select(t => t.PermissionId)
                                 .Distinct();

        var content = new Models.UserPermission
                      {
                          PermissionIds = ids
                      };

        return Task.FromResult(content);
    }

    private Task<Models.UserPermission> GetPermissionsAsync(GetUserPermission.PermissionCondition requestPermissionCondition, IEnumerable<Models.MixingPermissionRule> permissionRules, IEnumerable<Models.MixingPermissionCondition> mixingPermissionConditions)
    {
        Expression<Func<Models.MixingPermissionCondition, bool>> predicatePermissionCondition = t => true;

        predicatePermissionCondition = requestPermissionCondition.Group.IsEmpty()
                                           ? predicatePermissionCondition.And(t => t.Group.IsEmpty())
                                           : predicatePermissionCondition.And(t => t.Group.IsEmpty() || t.Group == requestPermissionCondition.Group);

        var conditions = mixingPermissionConditions.Where(predicatePermissionCondition.Compile())
                                                   .ToArray();

        var validatorCondition = Array.Empty<Models.Condition>();
        var keyCount = 0;

        if (!requestPermissionCondition.Conditions.IsEmpty())
        {
            var reqConditions = requestPermissionCondition.Conditions
                                                          .GroupBy(t => t.Key.ToUpper(), t => t.Value.ToUpper(), (key, values) => new
                                                                                                                                  {
                                                                                                                                      Key = key.ToUpper(),
                                                                                                                                      Values = values.Select(t => t.ToUpper())
                                                                                                                                  })
                                                          .ToArray();

            Expression<Func<Models.MixingPermissionCondition, bool>> predicateCondition = t => false;

            foreach (var i in reqConditions)
            {
                if (conditions.All(t => t.Key != i.Key)) continue;

                keyCount++;

                Expression<Func<Models.MixingPermissionCondition, bool>> predicateKey = t => t.Key == i.Key && (i.Values.Contains(t.Value) || t.Value == "*");

                predicateCondition = predicateCondition.Or(predicateKey);
            }

            if (keyCount > 0)
            {
                validatorCondition = conditions.Where(predicateCondition.Compile())
                                               .GroupBy(t => new { t.PermissionId, t.Priority }, t => t.Allowed)
                                               .Select(t =>
                                                       {
                                                           // 先將同權重的權限最大化
                                                           var p = t.Aggregate((c, n) => c || n);

                                                           return new
                                                                  {
                                                                      t.Key.PermissionId,
                                                                      t.Key.Priority,
                                                                      Allowed = p
                                                                  };
                                                       })
                                               .OrderBy(t => t.PermissionId).ThenBy(t => t.Priority)
                                               .GroupBy(t => new { t.PermissionId },
                                                        t => t.Allowed)
                                               .Select(t =>
                                                       {
                                                           // 最終以優先度高的權限為主
                                                           var p = t.Last();

                                                           return new
                                                                  {
                                                                      t.Key.PermissionId,
                                                                      Allowed = p
                                                                  };
                                                       })
                                               .GroupBy(t => t.PermissionId, t => t)
                                               .Select(t => new
                                                            {
                                                                PermissionId = t.Key,
                                                                Data = t,
                                                                Count = t.Count()
                                                            })
                                               .Where(t => t.Count >= keyCount)
                                               .Select(t =>
                                                       {
                                                           var p = t.Data.Select(t2 => t2.Allowed).Aggregate((c, n) => c && n);

                                                           return new Models.Condition
                                                                  {
                                                                      PermissionId = t.PermissionId,
                                                                      Allowed = p
                                                                  };
                                                       })
                                               .ToArray();
            }
        }

        var rules = permissionRules.GroupJoin(validatorCondition, t => t.PermissionId, t => t.PermissionId, (r, c) => new
                                                                                                                      {
                                                                                                                          Rule = r,
                                                                                                                          Conditions = c.DefaultIfEmpty()
                                                                                                                      })
                                   .SelectMany(t => t.Conditions.Select(t2 => new
                                                                              {
                                                                                  t.Rule.PermissionId,
                                                                                  RuleId = t.Rule.Id,
                                                                                  t.Rule.FunctionId,
                                                                                  t.Rule.PermissionType,
                                                                                  t.Rule.Priority,
                                                                                  t.Rule.Allowed,
                                                                                  Enabled = t2?.Allowed // ?? keyCount == 0
                                                                              }))
                                   .Where(t => t.Enabled != false)
                                   .GroupBy(t => new { t.PermissionId, t.FunctionId, t.Priority }, t => new { t.PermissionType, t.Allowed })
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
                                                          t.Key.PermissionId,
                                                          t.Key.FunctionId,
                                                          t.Key.Priority,
                                                          p.PermissionType,
                                                          p.Allowed
                                                      };
                                           })
                                   .ToArray();

        var validatorRules = rules.GroupBy(t => new { t.FunctionId, t.Priority }, t => new { t.PermissionType, t.Allowed })
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
                                          })
                                  .ToArray();

        var ids = rules.Join(validatorRules, r => r.FunctionId, f => f.FunctionId,
                             (r, f) => new
                                       {
                                           r.PermissionId,
                                           f.PermissionType
                                       })
                       .Where(t => t.PermissionType != PermissionType.None)
                       .Select(t => t.PermissionId)
                       .Distinct()
                       .ToArray();

        var content = new Models.UserPermission
                      {
                          PermissionIds = ids
                      };

        return Task.FromResult(content);
    }
}
using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Memory;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Permission.Queries;

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
            var userRoles = ds.Where(t => t.Id == request.UserId && !t.Role.Disabled).Select(t => t.RoleId);

            roleIds = userRoles.ToArray();
        }

        if (request.RoleId != null && request.RoleId.Any())
        {
            roleIds = roleIds.Union(request.RoleId).ToArray();
        }

        if (!roleIds.Any()) return Task.FromResult(Result.Forbidden);


        var cacheRolePermissionRule = _cache.Get<Dictionary<string, Models.RolePermissionRule>>(ConfigSettings.CACHE_ROLE_PERMISSION_RULE) ?? new Dictionary<string, Models.RolePermissionRule>();
        var cacheRolePermissionCondition = _cache.Get<Dictionary<long, Models.RolePermissionCondition>>(ConfigSettings.CACHE_ROLE_PERMISSION_CONDITION) ?? new Dictionary<long, Models.RolePermissionCondition>();

        if (!cacheRolePermissionRule.Any()) return Task.FromResult(Result.Forbidden);

        Expression<Func<KeyValuePair<string, Models.RolePermissionRule>, bool>> predicatePermissionRule = t => roleIds.Contains(t.Value.RoleId) && t.Value.FunctionId == request.FunctionId;
        Expression<Func<KeyValuePair<long, Models.RolePermissionCondition>, bool>> predicatePermissionCondition = t => roleIds.Contains(t.Value.RoleId);

        predicatePermissionCondition = request.Group.IsEmpty()
                                           ? predicatePermissionCondition.And(t => t.Value.Group.IsEmpty())
                                           : predicatePermissionCondition.And(t => t.Value.Group == request.Group);

        var conditions = cacheRolePermissionCondition.Where(predicatePermissionCondition.Compile())
                                                     .Select(t => t.Value)
                                                     .ToArray();

        var validatorCondition = Array.Empty<Models.Condition>();
        var keyCount = 0;

        if (!request.PermissionConditions.IsEmpty())
        {
            var reqConditions = request.PermissionConditions
                                       .GroupBy(t => t.Key, t => t.Value, (key, values) => new
                                                                                           {
                                                                                               Key = key,
                                                                                               Values = values
                                                                                           })
                                       .ToArray();

            Expression<Func<Models.RolePermissionCondition, bool>> predicateCondition = t => false;

            foreach (var i in reqConditions)
            {
                if (conditions.All(t => t.Key != i.Key)) continue;

                keyCount++;

                Expression<Func<Models.RolePermissionCondition, bool>> predicateKey = t => t.Key == i.Key && (i.Values.Contains(t.Value) || t.Value == "*");

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
                                                                      PermissionId = t.Key.PermissionId,
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

        var rules = cacheRolePermissionRule.Where(predicatePermissionRule.Compile())
                                           .Select(t => t.Value)
                                           .ToArray();

        if (!rules.Any()) return Task.FromResult(Result.Forbidden);


        var validatorRules = rules.GroupJoin(validatorCondition, t => t.PermissionId, t => t.PermissionId, (r, c) => new
                                                                                                                     {
                                                                                                                         Rule = r,
                                                                                                                         Conditions = c.DefaultIfEmpty()
                                                                                                                     })
                                  .SelectMany(t => t.Conditions.Select(t2 => new
                                                                             {
                                                                                 t.Rule.RoleId,
                                                                                 t.Rule.PermissionId,
                                                                                 RuleId = t.Rule.Id,
                                                                                 t.Rule.FunctionId,
                                                                                 t.Rule.PermissionType,
                                                                                 t.Rule.Priority,
                                                                                 t.Rule.Allowed,
                                                                                 Enabled = t2?.Allowed ?? keyCount == 0
                                                                             }))
                                  .Where(t => t.Enabled)
                                  .GroupBy(t => new { t.FunctionId, t.Priority }, t => new { t.PermissionType, t.Allowed })
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

        if (validatorRules.Any(t => (t.PermissionType & request.PermissionType) == request.PermissionType))
            return Task.FromResult(Result.Success);

        return Task.FromResult(Result.Forbidden);
    }
}
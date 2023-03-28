using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
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
    private readonly ConfigSettings _config;

    public ValidatePermissionHandler(DatabaseContext context, IMemoryCache cache, IOptions<ConfigSettings> config)
    {
        _context = context;
        _cache = cache;
        _config = config.Value;
    }

    public async Task<Result> Handle(ValidatePermission request, CancellationToken cancellationToken = default)
    {
        var cacheRolePermissionRule = _cache.Get<Dictionary<string, Models.RolePermissionRule>>(ConfigSettings.CACHE_ROLE_PERMISSION_RULE) ?? new Dictionary<string, Models.RolePermissionRule>();
        var cacheRolePermissionCondition = _cache.Get<Dictionary<long, Models.RolePermissionCondition>>(ConfigSettings.CACHE_ROLE_PERMISSION_CONDITION) ?? new Dictionary<long, Models.RolePermissionCondition>();
        var cacheUserPermissionCondition = _cache.Get<Dictionary<long, Models.UserPermissionCondition>>(ConfigSettings.CACHE_USER_PERMISSION_CONDITION) ?? new Dictionary<long, Models.UserPermissionCondition>();

        if (!cacheRolePermissionRule.Any())
            return Result.Forbidden;

        var roleIds = Array.Empty<long>();

        if (request.UserId.HasValue)
        {
            if (_config.AppSettings.Owner?.Any(t => t == request.UserId) ?? false)
                return Result.Success;

            var dsUser = _context.Set<Domain.Entities.User>();

            var user = await dsUser.Where(t => t.Id == request.UserId.Value)
                                   .Select(t => new
                                                {
                                                    t.Id,
                                                    t.Disabled,
                                                    Roles = t.Roles
                                                             .Where(t2 => t2.ExpireDate == null || t2.ExpireDate > DateTimeOffset.UtcNow)
                                                             .Select(t2 => t2.RoleId)
                                                })
                                   .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
                return Result.Forbidden;

            if (user.Disabled)
                return Result.AccountIsDisabled;

            if (!user.Roles.Any())
                return Result.Forbidden;

            roleIds = user.Roles.ToArray();
        }

        if (request.RoleId != null && request.RoleId.Any())
        {
            roleIds = roleIds.Union(request.RoleId).ToArray();
        }

        if (request.RoleExtendData != null && request.RoleExtendData.Any())
        {
            Expression<Func<Domain.Entities.Role, bool>> predicateRole = p => false;

            var extendData = request.RoleExtendData.GroupBy(t => t.Key, (k, values) =>
                                                                            new
                                                                            {
                                                                                Key = k.ToUpper(),
                                                                                Values = values.Select(t => t.Value.ToUpper())
                                                                            });

            predicateRole = extendData.Aggregate(predicateRole, (current, item) => current.Or(t => t.ExtendData.Any(t2 => t2.Key == item.Key && item.Values.Contains(t2.Value))));

            var dsRole = _context.Set<Domain.Entities.Role>();

            var roleIdFilter = await dsRole.Where(predicateRole)
                                           .Select(t => t.Id)
                                           .ToArrayAsync(cancellationToken);

            roleIds = roleIds.Where(t => roleIdFilter.Contains(t)).ToArray();
        }

        roleIds = roleIds.Distinct().ToArray();

        if (!roleIds.Any()) return Result.Forbidden;

        Expression<Func<KeyValuePair<string, Models.RolePermissionRule>, bool>> predicatePermissionRule = t => roleIds.Contains(t.Value.RoleId) && t.Value.FunctionId == request.FunctionId;
        Expression<Func<KeyValuePair<long, Models.RolePermissionCondition>, bool>> predicateRolePermissionCondition = t => roleIds.Contains(t.Value.RoleId);
        Expression<Func<KeyValuePair<long, Models.UserPermissionCondition>, bool>> predicateUserPermissionCondition = t => t.Value.UserId == request.UserId && (t.Value.ExpireDate == null || t.Value.ExpireDate > DateTimeOffset.UtcNow);

        predicateRolePermissionCondition = request.Group.IsEmpty()
                                               ? predicateRolePermissionCondition.And(t => t.Value.Group.IsEmpty())
                                               : predicateRolePermissionCondition.And(t => t.Value.Group.IsEmpty() || t.Value.Group == request.Group);

        predicateUserPermissionCondition = request.Group.IsEmpty()
                                               ? predicateUserPermissionCondition.And(t => t.Value.Group.IsEmpty())
                                               : predicateUserPermissionCondition.And(t => t.Value.Group.IsEmpty() || t.Value.Group == request.Group);

        var roleConditions = cacheRolePermissionCondition.Where(predicateRolePermissionCondition.Compile())
                                                         .Select(t => new Models.PermissionCondition

                                                                      {
                                                                          PermissionId = t.Value.PermissionId,
                                                                          Priority = t.Value.Priority,
                                                                          Group = t.Value.Group,
                                                                          Key = t.Value.Key,
                                                                          Value = t.Value.Value,
                                                                          Allowed = t.Value.Allowed
                                                                      })
                                                         .ToArray();

        var userConditions = cacheUserPermissionCondition.Where(predicateUserPermissionCondition.Compile())
                                                         .Select(t => new Models.PermissionCondition

                                                                      {
                                                                          PermissionId = t.Value.PermissionId,
                                                                          Priority = t.Value.Priority,
                                                                          Group = t.Value.Group,
                                                                          Key = t.Value.Key,
                                                                          Value = t.Value.Value,
                                                                          Allowed = t.Value.Allowed
                                                                      })
                                                         .ToArray();

        var conditions = roleConditions.Union(userConditions)
                                       .Distinct()
                                       .ToArray();

        var validatorCondition = Array.Empty<Models.Condition>();
        var keyCount = 0;

        if (!request.PermissionConditions.IsEmpty())
        {
            var reqConditions = request.PermissionConditions
                                       .GroupBy(t => t.Key.ToUpper(), t => t.Value.ToUpper(), (key, values) => new
                                                                                                               {
                                                                                                                   Key = key.ToUpper(),
                                                                                                                   Values = values.Select(t => t.ToUpper())
                                                                                                               })
                                       .ToArray();

            Expression<Func<Models.PermissionCondition, bool>> predicateCondition = t => false;

            foreach (var i in reqConditions)
            {
                if (conditions.All(t => t.Key != i.Key)) continue;

                keyCount++;

                Expression<Func<Models.PermissionCondition, bool>> predicateKey = t => t.Key == i.Key && (i.Values.Contains(t.Value) || t.Value == "*");

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

        if (!rules.Any()) return Result.Forbidden;


        var validatorRules = rules.GroupJoin(validatorCondition, t => t.PermissionId, t => t.PermissionId, (r, c) => new
                                                                                                                     {
                                                                                                                         Rule = r,
                                                                                                                         Conditions = c.DefaultIfEmpty()
                                                                                                                     })
                                  .SelectMany(t => t.Conditions.Select(t2 =>
                                                                       {
                                                                           return new
                                                                                  {
                                                                                      t.Rule.RoleId,
                                                                                      t.Rule.PermissionId,
                                                                                      RuleId = t.Rule.Id,
                                                                                      t.Rule.FunctionId,
                                                                                      t.Rule.PermissionType,
                                                                                      t.Rule.Priority,
                                                                                      t.Rule.Allowed,
                                                                                      Enabled = t2?.Allowed ?? t.Rule.Allowed
                                                                                  };
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
            return Result.Success;

        return Result.Forbidden;
    }
}
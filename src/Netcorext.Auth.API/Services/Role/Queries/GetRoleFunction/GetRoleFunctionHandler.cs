using System.Linq.Expressions;
using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role.Queries;

public class GetRoleFunctionHandler : IRequestHandler<GetRoleFunction, Result<IEnumerable<Models.RoleFunction>>>
{
    private readonly DatabaseContext _context;

    public GetRoleFunctionHandler(DatabaseContextAdapter context)
    {
        _context = context.Slave;
    }

    public async Task<Result<IEnumerable<Models.RoleFunction>>> Handle(GetRoleFunction request, CancellationToken cancellationToken = default)
    {
        var dsRole = _context.Set<Domain.Entities.Role>();
        var dsRolePermission = _context.Set<Domain.Entities.RolePermission>();
        var dsRolePermissionCondition = _context.Set<Domain.Entities.RolePermissionCondition>();
        var dsRule = _context.Set<Domain.Entities.Rule>();
        var emptyContent = Array.Empty<Models.RoleFunction>();

        var roleIds = dsRole.Where(t => request.Ids.Contains(t.Id) && !t.Disabled)
                            .Select(t => t.Id)
                            .ToArray();

        if (roleIds.Length == 0)
            return Result<IEnumerable<Models.RoleFunction>>.Success.Clone(emptyContent);

        var permissionIds = dsRolePermission.Where(t => roleIds.Contains(t.Id))
                                            .Select(t => t.PermissionId)
                                            .ToArray();

        if (permissionIds.Length == 0)
            return Result<IEnumerable<Models.RoleFunction>>.Success.Clone(emptyContent);

        var rules = dsRule.Where(t => permissionIds.Contains(t.PermissionId))
                          .Select(t => new Models.MixingPermissionRule
                                       {
                                           Id = t.Id,
                                           PermissionId = t.PermissionId,
                                           FunctionId = t.FunctionId,
                                           Priority = t.Permission.Priority,
                                           PermissionType = t.PermissionType,
                                           Allowed = t.Allowed
                                       })
                          .ToArray();

        if (rules.Length == 0)
            return Result<IEnumerable<Models.RoleFunction>>.Success.Clone(emptyContent);

        var groups = request.PermissionConditions?
                            .Where(t => !t.Group.IsEmpty())
                            .Select(t => t.Group!)
                            .Distinct()
                            .ToArray() ?? Array.Empty<string>();

        var rolePermissionConditions = dsRolePermissionCondition.Where(t => roleIds.Contains(t.RoleId) && (t.Group == null || groups.Contains(t.Group)))
                                                                .Select(t => new Models.MixingPermissionCondition
                                                                             {
                                                                                 PermissionId = t.PermissionId,
                                                                                 Priority = t.Priority,
                                                                                 Group = t.Group,
                                                                                 Key = t.Key,
                                                                                 Value = t.Value,
                                                                                 Allowed = t.Allowed
                                                                             })
                                                                .ToArray();

        Models.RoleFunction[] content;

        if (request.PermissionConditions == null || !request.PermissionConditions.Any())
        {
            content = new[] { await GetFunctionsWithoutConditionAsync(rules) };

            return Result<IEnumerable<Models.RoleFunction>>.Success.Clone(content);
        }

        content = new Models.RoleFunction[request.PermissionConditions.Length];

        async void Handler(int i)
        {
            content[i] = await GetFunctionsAsync(request.PermissionConditions[i], rules, rolePermissionConditions);
        }

        Parallel.For(0, request.PermissionConditions.Length, Handler);

        return Result<IEnumerable<Models.RoleFunction>>.Success.Clone(content);
    }

    private Task<Models.RoleFunction> GetFunctionsWithoutConditionAsync(IEnumerable<Models.MixingPermissionRule> permissions)
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

        if (!permissionRules.Any()) return Task.FromResult(new Models.RoleFunction());

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
                                                              PermissionType = p.Allowed ? p.PermissionType : PermissionType.None
                                                          };
                                               });

        var fns = permissionRules.Join(functions, r => r.FunctionId, f => f.FunctionId,
                                       (r, f) => f with
                                                 {
                                                     PermissionType = r.PermissionType & f.PermissionType
                                                 })
                                 .Where(t => t.PermissionType != PermissionType.None)
                                 .Select(t => new Models.Function
                                              {
                                                  Id = t.FunctionId,
                                                  PermissionType = t.PermissionType
                                              })
                                 .DistinctBy(t => new { t.Id, t.PermissionType })
                                 .OrderBy(t => t.Id);

        ;

        var content = new Models.RoleFunction
                      {
                          Functions = fns
                      };

        return Task.FromResult(content);
    }

    private Task<Models.RoleFunction> GetFunctionsAsync(GetRoleFunction.PermissionCondition requestPermissionCondition, IEnumerable<Models.MixingPermissionRule> permissionRules, IEnumerable<Models.MixingPermissionCondition> mixingPermissionConditions)
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
                                                         PermissionType = p.Allowed ? p.PermissionType : PermissionType.None
                                                     };
                                          })
                                  .ToArray();

        var fns = rules.Join(validatorRules, r => r.FunctionId, f => f.FunctionId,
                             (r, f) => new
                                       {
                                           f.FunctionId,
                                           f.PermissionType
                                       })
                       .Where(t => t.PermissionType != PermissionType.None)
                       .Select(t => new Models.Function
                                    {
                                        Id = t.FunctionId,
                                        PermissionType = t.PermissionType
                                    })
                       .DistinctBy(t => new { t.Id, t.PermissionType })
                       .OrderBy(t => t.Id);

        ;

        var content = new Models.RoleFunction
                      {
                          Functions = fns
                      };

        return Task.FromResult(content);
    }
}
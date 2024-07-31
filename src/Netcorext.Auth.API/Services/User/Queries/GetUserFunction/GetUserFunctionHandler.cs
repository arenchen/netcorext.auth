using System.Linq.Expressions;
using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserFunctionHandler : IRequestHandler<GetUserFunction, Result<IEnumerable<Models.UserFunction>>>
{
    private readonly DatabaseContext _context;

    public GetUserFunctionHandler(DatabaseContextAdapter context)
    {
        _context = context.Slave;
    }

    public async Task<Result<IEnumerable<Models.UserFunction>>> Handle(GetUserFunction request, CancellationToken cancellationToken = default)
    {
        var dsUserRole = _context.Set<Domain.Entities.UserRole>();
        var dsUserPermissionCondition = _context.Set<Domain.Entities.UserPermissionCondition>();
        var dsPermission = _context.Set<Domain.Entities.Permission>();
        var dsRolePermission = _context.Set<Domain.Entities.RolePermission>();
        var dsRolePermissionCondition = _context.Set<Domain.Entities.RolePermissionCondition>();
        var emptyContent = new Models.UserFunction[request.PermissionConditions?.Length ?? 0];

        var roleIds = dsUserRole.Where(t => t.Id == request.Id && !t.User.Disabled && !t.Role.Disabled && t.ExpireDate > DateTimeOffset.UtcNow)
                                .Select(t => t.RoleId)
                                .ToArray();

        if (roleIds.Length == 0)
            return Result<IEnumerable<Models.UserFunction>>.Success.Clone(emptyContent);

        var rolePermissions = dsRolePermission.Where(t => !t.Permission.Disabled && roleIds.Contains(t.Id))
                                              .Select(t => t.PermissionId)
                                              .ToArray();

        Models.UserFunction[] content;
        Models.PermissionRule[] rules;

        if (request.PermissionConditions == null || request.PermissionConditions.Length == 0)
        {
            rules = dsPermission.Where(t => rolePermissions.Contains(t.Id))
                                .SelectMany(t => t.Rules.Select(t2 => new Models.PermissionRule
                                                                      {
                                                                          Id = t2.Id,
                                                                          PermissionId = t.Id,
                                                                          FunctionId = t2.FunctionId,
                                                                          Priority = t.Priority,
                                                                          PermissionType = t2.PermissionType,
                                                                          Allowed = t2.Allowed
                                                                      }))
                                .ToArray();

            content = new[] { await GetFunctionsWithoutConditionAsync(rules) };

            return Result<IEnumerable<Models.UserFunction>>.Success.Clone(content);
        }

        var groups = request.PermissionConditions?
                            .Where(t => !t.Group.IsEmpty())
                            .Select(t => t.Group!)
                            .Distinct()
                            .ToArray() ?? Array.Empty<string>();

        var rolePermissionConditions = dsRolePermissionCondition.Where(t => roleIds.Contains(t.RoleId) && (t.Group == null || groups.Contains(t.Group)))
                                                                .Select(t => new Models.PermissionCondition
                                                                             {
                                                                                 PermissionId = t.PermissionId,
                                                                                 Group = t.Group,
                                                                                 Key = t.Key,
                                                                                 Value = t.Value
                                                                             })
                                                                .ToArray();

        var userPermissionConditions = dsUserPermissionCondition.Where(t => t.UserId == request.Id && t.ExpireDate > DateTimeOffset.UtcNow && (t.Group == null || groups.Contains(t.Group)))
                                                                .Select(t => new Models.PermissionCondition
                                                                             {
                                                                                 PermissionId = t.PermissionId,
                                                                                 Group = t.Group,
                                                                                 Key = t.Key,
                                                                                 Value = t.Value
                                                                             });

        var conditions = rolePermissionConditions.Union(userPermissionConditions)
                                                 .Distinct()
                                                 .ToArray();

        var maxRangePermissions = rolePermissions.Union(conditions.Select(t => t.PermissionId))
                                                 .ToArray();

        rules = dsPermission.Where(t => maxRangePermissions.Contains(t.Id))
                            .SelectMany(t => t.Rules.Select(t2 => new Models.PermissionRule
                                                                  {
                                                                      Id = t2.Id,
                                                                      PermissionId = t.Id,
                                                                      FunctionId = t2.FunctionId,
                                                                      Priority = t.Priority,
                                                                      PermissionType = t2.PermissionType,
                                                                      Allowed = t2.Allowed
                                                                  }))
                            .ToArray();

        content = new Models.UserFunction[request.PermissionConditions?.Length ?? 0];

        async void Handler(int i)
        {
            if (request.PermissionConditions == null || request.PermissionConditions.Length == 0)
                return;

            content[i] = await GetFunctionsAsync(request.PermissionConditions[i], rolePermissions, conditions, rules);
        }

        Parallel.For(0, content.Length, Handler);

        return Result<IEnumerable<Models.UserFunction>>.Success.Clone(content);
    }

    private Task<Models.UserFunction> GetFunctionsWithoutConditionAsync(IEnumerable<Models.PermissionRule> rules)
    {
        var validatorRules = rules.Select(t => new
                                               {
                                                   t.FunctionId,
                                                   t.Priority,
                                                   Readable = t.PermissionType.HasFlag(PermissionType.Read) ? (bool?)t.Allowed : null,
                                                   Writable = t.PermissionType.HasFlag(PermissionType.Write) ? (bool?)t.Allowed : null,
                                                   Deletable = t.PermissionType.HasFlag(PermissionType.Delete) ? (bool?)t.Allowed : null
                                               })
                                  .GroupBy(t => new { t.FunctionId, t.Priority }, t => new { t.Readable, t.Writable, t.Deletable })
                                  .Select(t =>
                                          {
                                              // 先將同權重允許/不允許的權限最大化
                                              var p = t.Aggregate((c, n) =>
                                                                  {
                                                                      var readable = c.Readable.HasValue
                                                                                         ? c.Readable | (n.Readable ?? c.Readable)
                                                                                         : n.Readable;

                                                                      var writable = c.Writable.HasValue
                                                                                         ? c.Writable | (n.Writable ?? c.Writable)
                                                                                         : n.Writable;

                                                                      var deletable = c.Deletable.HasValue
                                                                                          ? c.Deletable | (n.Deletable ?? c.Deletable)
                                                                                          : n.Deletable;

                                                                      return new
                                                                             {
                                                                                 Readable = readable,
                                                                                 Writable = writable,
                                                                                 Deletable = deletable
                                                                             };
                                                                  });

                                              return new
                                                     {
                                                         t.Key.FunctionId,
                                                         t.Key.Priority,
                                                         p.Readable,
                                                         p.Writable,
                                                         p.Deletable
                                                     };
                                          })
                                  .OrderBy(t => t.FunctionId).ThenBy(t => t.Priority)
                                  .GroupBy(t => t.FunctionId,
                                           t => new { t.Readable, t.Writable, t.Deletable })
                                  .Select(t =>
                                          {
                                              // 最終以優先度高的權限為主
                                              var p = t.Aggregate((c, n) =>
                                                                  {
                                                                      var readable = n.Readable ?? c.Readable;

                                                                      var writable = n.Writable ?? c.Writable;

                                                                      var deletable = n.Deletable ?? c.Deletable;

                                                                      return new
                                                                             {
                                                                                 Readable = readable,
                                                                                 Writable = writable,
                                                                                 Deletable = deletable
                                                                             };
                                                                  });

                                              return new
                                                     {
                                                         FunctionId = t.Key,
                                                         PermissionType = PermissionType.None
                                                                        | (p.Readable.HasValue && p.Readable.Value ? PermissionType.Read : PermissionType.None)
                                                                        | (p.Writable.HasValue && p.Writable.Value ? PermissionType.Write : PermissionType.None)
                                                                        | (p.Deletable.HasValue && p.Deletable.Value ? PermissionType.Delete : PermissionType.None)
                                                     };
                                          })
                                  .ToArray();

        var result = new Models.UserFunction
                     {
                         Functions = validatorRules.Where(t => t.PermissionType != PermissionType.None)
                                                   .Select(t => new Models.Function
                                                                {
                                                                    Id = t.FunctionId,
                                                                    PermissionType = t.PermissionType
                                                                })
                     };

        return Task.FromResult(result);
    }

    private Task<Models.UserFunction> GetFunctionsAsync(GetUserFunction.PermissionCondition requestPermissionCondition, IEnumerable<long> rolePermissions, IEnumerable<Models.PermissionCondition> mixingPermissionConditions, IEnumerable<Models.PermissionRule> rules)
    {
        Expression<Func<Models.PermissionCondition, bool>> predicatePermissionCondition = t => requestPermissionCondition.Group.IsEmpty()
                                                                                                   ? t.Group.IsEmpty()
                                                                                                   : t.Group.IsEmpty() || t.Group == requestPermissionCondition.Group;

        var conditions = mixingPermissionConditions.Where(predicatePermissionCondition.Compile()).ToArray();

        if (!requestPermissionCondition.Conditions.IsEmpty())
        {
            var reqConditions = requestPermissionCondition.Conditions
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

                Expression<Func<Models.PermissionCondition, bool>> predicateKey = t => t.Key == i.Key && (i.Values.Contains(t.Value) || t.Value == "*");

                predicateCondition = predicateCondition.Or(predicateKey);
            }


            if (reqConditions.Any())
            {
                conditions = conditions.Where(predicateCondition.Compile()).ToArray();
            }
        }

        var permissions = conditions.Select(t => t.PermissionId)
                                    .Union(rolePermissions)
                                    .Distinct();

        var permissionRules = rules.Where(t => permissions.Contains(t.PermissionId))
                                   .ToArray();

        var validatorRules = permissionRules.Select(t => new
                                                         {
                                                             t.FunctionId,
                                                             t.Priority,
                                                             Readable = t.PermissionType.HasFlag(PermissionType.Read) ? (bool?)t.Allowed : null,
                                                             Writable = t.PermissionType.HasFlag(PermissionType.Write) ? (bool?)t.Allowed : null,
                                                             Deletable = t.PermissionType.HasFlag(PermissionType.Delete) ? (bool?)t.Allowed : null
                                                         })
                                            .GroupBy(t => new { t.FunctionId, t.Priority }, t => new { t.Readable, t.Writable, t.Deletable })
                                            .Select(t =>
                                                    {
                                                        // 先將同權重允許/不允許的權限最大化
                                                        var p = t.Aggregate((c, n) =>
                                                                            {
                                                                                var readable = c.Readable.HasValue
                                                                                                   ? c.Readable | (n.Readable ?? c.Readable)
                                                                                                   : n.Readable;

                                                                                var writable = c.Writable.HasValue
                                                                                                   ? c.Writable | (n.Writable ?? c.Writable)
                                                                                                   : n.Writable;

                                                                                var deletable = c.Deletable.HasValue
                                                                                                    ? c.Deletable | (n.Deletable ?? c.Deletable)
                                                                                                    : n.Deletable;

                                                                                return new
                                                                                       {
                                                                                           Readable = readable,
                                                                                           Writable = writable,
                                                                                           Deletable = deletable
                                                                                       };
                                                                            });

                                                        return new
                                                               {
                                                                   t.Key.FunctionId,
                                                                   t.Key.Priority,
                                                                   p.Readable,
                                                                   p.Writable,
                                                                   p.Deletable
                                                               };
                                                    })
                                            .OrderBy(t => t.FunctionId).ThenBy(t => t.Priority)
                                            .GroupBy(t => t.FunctionId,
                                                     t => new { t.Readable, t.Writable, t.Deletable })
                                            .Select(t =>
                                                    {
                                                        // 最終以優先度高的權限為主
                                                        var p = t.Aggregate((c, n) =>
                                                                            {
                                                                                var readable = n.Readable ?? c.Readable;

                                                                                var writable = n.Writable ?? c.Writable;

                                                                                var deletable = n.Deletable ?? c.Deletable;

                                                                                return new
                                                                                       {
                                                                                           Readable = readable,
                                                                                           Writable = writable,
                                                                                           Deletable = deletable
                                                                                       };
                                                                            });

                                                        return new
                                                               {
                                                                   FunctionId = t.Key,
                                                                   PermissionType = PermissionType.None
                                                                                  | (p.Readable.HasValue && p.Readable.Value ? PermissionType.Read : PermissionType.None)
                                                                                  | (p.Writable.HasValue && p.Writable.Value ? PermissionType.Write : PermissionType.None)
                                                                                  | (p.Deletable.HasValue && p.Deletable.Value ? PermissionType.Delete : PermissionType.None)
                                                               };
                                                    })
                                            .ToArray();

        var result = new Models.UserFunction
                     {
                         Functions = validatorRules.Where(t => t.PermissionType != PermissionType.None)
                                                   .Select(t => new Models.Function
                                                                {
                                                                    Id = t.FunctionId,
                                                                    PermissionType = t.PermissionType
                                                                })
                     };

        return Task.FromResult(result);
    }
}

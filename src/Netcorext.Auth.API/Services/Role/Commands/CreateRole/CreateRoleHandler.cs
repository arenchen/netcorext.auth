using Netcorext.Algorithms;
using Netcorext.Auth.Domain.Entities;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role.Commands;

public class CreateRoleHandler : IRequestHandler<CreateRole, Result<IEnumerable<long>>>
{
    private readonly DatabaseContext _context;
    private readonly ISnowflake _snowflake;

    public CreateRoleHandler(DatabaseContextAdapter context, ISnowflake snowflake)
    {
        _context = context;
        _snowflake = snowflake;
    }

    public async Task<Result<IEnumerable<long>>> Handle(CreateRole request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Role>();
        var dsPermission = _context.Set<Domain.Entities.Permission>();

        if (request.Roles.SelectMany(t => t.Permissions ?? Array.Empty<CreateRole.RolePermission>()).Any())
        {
            var names = request.Roles.Select(t => t.Name.ToUpper());

            if (ds.Any(t => names.Contains(t.Name.ToUpper())))
                return Result<IEnumerable<long>>.Conflict;

            var permissionIds = request.Roles.SelectMany(t => t.Permissions ?? Array.Empty<CreateRole.RolePermission>()).Select(t => t.PermissionId).ToArray();

            if (dsPermission.Count(t => permissionIds.Contains(t.Id)) != permissionIds.Length)
                return Result<IEnumerable<long>>.DependencyNotFound;

            permissionIds = request.Roles.SelectMany(t => t.PermissionConditions ?? Array.Empty<CreateRole.RolePermissionCondition>()).Select(t => t.PermissionId).Distinct().ToArray();

            if (dsPermission.Count(t => permissionIds.Contains(t.Id)) != permissionIds.Length)
                return Result<IEnumerable<long>>.DependencyNotFound;
        }

        var states = request.Roles
                            .SelectMany(t => t.PermissionFromStates ?? Array.Empty<string>())
                            .Where(t => !string.IsNullOrWhiteSpace(t))
                            .ToArray();

        var permissionExtends = dsPermission.Where(t => !t.Disabled && states.Contains(t.State))
                                            .ToDictionary(t => t.State ?? "", t => t.Id);

        var entities = request.Roles.Select(t =>
                                            {
                                                var id = t.CustomId ?? _snowflake.Generate();

                                                var permissions = permissionExtends.Where(t2 => (t.PermissionFromStates ?? Array.Empty<string>()).Contains(t2.Key))
                                                                                   .Select(t2 => new RolePermission
                                                                                                 {
                                                                                                     Id = id,
                                                                                                     PermissionId = t2.Value
                                                                                                 })
                                                                                   .ToArray();

                                                return new Domain.Entities.Role
                                                       {
                                                           Id = id,
                                                           Name = t.Name,
                                                           Priority = t.Priority,
                                                           Disabled = t.Disabled,
                                                           ExtendData = t.ExtendData?
                                                                         .Select(t2 => new RoleExtendData
                                                                                       {
                                                                                           Id = id,
                                                                                           Key = t2.Key.ToUpper(),
                                                                                           Value = t2.Value.ToUpper()
                                                                                       })
                                                                         .ToArray() ?? Array.Empty<RoleExtendData>(),
                                                           Permissions = permissions.Union(t.Permissions?
                                                                                            .Select(t2 => new RolePermission
                                                                                                          {
                                                                                                              Id = id,
                                                                                                              PermissionId = t2.PermissionId
                                                                                                          })
                                                                                            .ToArray() ?? Array.Empty<RolePermission>())
                                                                                    .ToArray(),
                                                           PermissionConditions = t.PermissionConditions?
                                                                                   .Select(t => new RolePermissionCondition
                                                                                                {
                                                                                                    Id = _snowflake.Generate(),
                                                                                                    RoleId = id,
                                                                                                    PermissionId = t.PermissionId,
                                                                                                    Group = t.Group?.ToUpper(),
                                                                                                    Key = t.Key.ToUpper(),
                                                                                                    Value = t.Value.ToUpper()
                                                                                                })
                                                                                   .ToArray() ?? Array.Empty<RolePermissionCondition>()
                                                       };
                                            })
                              .ToArray();

        await ds.AddRangeAsync(entities, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<IEnumerable<long>>.SuccessCreated.Clone(entities.Select(t => t.Id));
    }
}

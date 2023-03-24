using Microsoft.EntityFrameworkCore;
using Netcorext.Algorithms;
using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.EntityFramework.UserIdentityPattern.Extensions;
using Netcorext.Extensions.Commons;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role.Commands;

public class UpdateRoleHandler : IRequestHandler<UpdateRole, Result>
{
    private readonly DatabaseContext _context;
    private readonly ISnowflake _snowflake;

    public UpdateRoleHandler(DatabaseContext context, ISnowflake snowflake)
    {
        _context = context;
        _snowflake = snowflake;
    }

    public async Task<Result> Handle(UpdateRole request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Role>();
        var dsExtendData = _context.Set<Domain.Entities.RoleExtendData>();
        var dsPermission = _context.Set<Domain.Entities.Permission>();
        var dsRolePermission = _context.Set<Domain.Entities.RolePermission>();
        var dsPermissionCondition = _context.Set<Domain.Entities.RolePermissionCondition>();

        if (!await ds.AnyAsync(t => t.Id == request.Id, cancellationToken))
            return Result.NotFound;

        if (!request.Name.IsEmpty())
            if (await ds.AnyAsync(t => t.Id != request.Id && t.Name.ToUpper() == request.Name.ToUpper(), cancellationToken))
                return Result.Conflict;

        if (request.Permissions != null && request.Permissions.Any())
        {
            var permissionIds = request.Permissions
                                       .Select(t => t.PermissionId)
                                       .ToArray();

            if (dsPermission.Count(t => permissionIds.Contains(t.Id)) != permissionIds.Length)
                return Result.DependencyNotFound;
        }

        if (request.PermissionConditions != null && request.PermissionConditions.Any())
        {
            var permissionIds = request.PermissionConditions
                                       .Select(t => t.PermissionId)
                                       .ToArray();

            if (dsPermission.Count(t => permissionIds.Contains(t.Id)) != permissionIds.Length)
                return Result.DependencyNotFound;
        }

        var entity = ds.Include(t => t.ExtendData)
                       .Include(t => t.Permissions)
                       .Include(t => t.PermissionConditions)
                       .First(t => t.Id == request.Id);

        _context.Entry(entity).UpdateProperty(t => t.Name, request.Name);
        _context.Entry(entity).UpdateProperty(t => t.Disabled, request.Disabled);

        if (request.ExtendData != null && request.ExtendData.Any())
        {
            // 先將請求資料依照 CRUD 進行分組
            var gExtendData = request.ExtendData
                                     .GroupBy(t => t.Crud, (mode, data) => new
                                                                           {
                                                                               Mode = mode,
                                                                               Data = data.Select(t => new Domain.Entities.RoleExtendData
                                                                                                       {
                                                                                                           Id = entity.Id,
                                                                                                           Key = t.Key.ToUpper(),
                                                                                                           Value = t.Value.ToUpper()
                                                                                                       })
                                                                                          .ToArray()
                                                                           })
                                     .ToArray();

            var createExtendData = gExtendData.FirstOrDefault(t => t.Mode == CRUD.C)?.Data ?? Array.Empty<Domain.Entities.RoleExtendData>();
            var updateExtendData = gExtendData.FirstOrDefault(t => t.Mode == CRUD.U)?.Data ?? Array.Empty<Domain.Entities.RoleExtendData>();
            var deleteExtendData = gExtendData.FirstOrDefault(t => t.Mode == CRUD.D)?.Data ?? Array.Empty<Domain.Entities.RoleExtendData>();

            var extendData = entity.ExtendData
                                   .Join(deleteExtendData, t => new { t.Id, t.Key }, t => new { t.Id, t.Key }, (src, desc) => src)
                                   .ToArray();

            if (extendData.Any()) dsExtendData.RemoveRange(extendData);

            if (createExtendData.Any()) dsExtendData.AddRange(createExtendData);

            extendData = entity.ExtendData
                               .Join(updateExtendData, t => new { t.Id, t.Key }, t => new { t.Id, t.Key },
                                     (src, desc) =>
                                     {
                                         src.Value = desc.Value.ToUpper();

                                         return src;
                                     })
                               .ToArray();

            dsExtendData.UpdateRange(extendData);
        }

        if (request.Permissions != null && request.Permissions.Any())
        {
            // 先將請求資料依照 CRUD 進行分組
            var gPermissions = request.Permissions
                                      .GroupBy(t => t.Crud, (mode, data) => new
                                                                            {
                                                                                Mode = mode,
                                                                                Data = data.Select(t => new Domain.Entities.RolePermission
                                                                                                        {
                                                                                                            Id = request.Id,
                                                                                                            PermissionId = t.PermissionId
                                                                                                        })
                                                                                           .ToArray()
                                                                            })
                                      .ToArray();

            var createPermission = gPermissions.FirstOrDefault(t => t.Mode == CRUD.C)?.Data;

            // var updatePermission = gPermissions.FirstOrDefault(t => t.Mode == CRUD.U)?.Data;
            var deletePermission = gPermissions.FirstOrDefault(t => t.Mode == CRUD.D)?.Data;

            if (deletePermission != null && deletePermission.Any())
            {
                var permissions = entity.Permissions
                                        .Join(deletePermission, t => new { t.Id, t.PermissionId }, t => new { t.Id, t.PermissionId }, (src, desc) => src)
                                        .ToArray();

                if (permissions.Any()) dsRolePermission.RemoveRange(permissions);
            }

            if (createPermission != null && createPermission.Any())
            {
                if (createPermission.Any())
                    dsRolePermission.AddRange(createPermission);
            }
        }

        if (request.PermissionConditions != null && request.PermissionConditions.Any())
        {
            var gPermissionCondition = request.PermissionConditions
                                              .GroupBy(t => t.Crud, (mode, permissionCondition) => new
                                                                                                   {
                                                                                                       Mode = mode,
                                                                                                       Data = permissionCondition.Select(t => new Domain.Entities.RolePermissionCondition
                                                                                                                                              {
                                                                                                                                                  Id = t.Id ?? _snowflake.Generate(),
                                                                                                                                                  RoleId = entity.Id,
                                                                                                                                                  PermissionId = t.PermissionId,
                                                                                                                                                  Priority = t.Priority,
                                                                                                                                                  Group = t.Group?.ToUpper(),
                                                                                                                                                  Key = t.Key.ToUpper(),
                                                                                                                                                  Value = t.Value,
                                                                                                                                                  Allowed = t.Allowed
                                                                                                                                              })
                                                                                                                                 .ToArray()
                                                                                                   })
                                              .ToArray();

            var createPermissionCondition = gPermissionCondition.FirstOrDefault(t => t.Mode == CRUD.C)?.Data ?? Array.Empty<Domain.Entities.RolePermissionCondition>();
            var updatePermissionCondition = gPermissionCondition.FirstOrDefault(t => t.Mode == CRUD.U)?.Data ?? Array.Empty<Domain.Entities.RolePermissionCondition>();
            var deletePermissionCondition = gPermissionCondition.FirstOrDefault(t => t.Mode == CRUD.D)?.Data ?? Array.Empty<Domain.Entities.RolePermissionCondition>();

            var permissionCondition = entity.PermissionConditions
                                            .Join(deletePermissionCondition, t => t.Id, t => t.Id, (src, desc) => src)
                                            .ToArray();

            if (permissionCondition.Any()) dsPermissionCondition.RemoveRange(permissionCondition);

            if (createPermissionCondition.Any()) dsPermissionCondition.AddRange(createPermissionCondition);

            permissionCondition = entity.PermissionConditions
                                        .Join(updatePermissionCondition, t => t.Id, t => t.Id,
                                              (o, i) =>
                                              {
                                                  o.PermissionId = i.PermissionId;
                                                  o.Priority = i.Priority;
                                                  o.Group = i.Group;
                                                  o.Value = i.Value;
                                                  o.Allowed = i.Allowed;

                                                  return o;
                                              })
                                        .ToArray();

            dsPermissionCondition.UpdateRange(permissionCondition);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.SuccessNoContent;
    }
}
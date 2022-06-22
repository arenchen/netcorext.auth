using Microsoft.EntityFrameworkCore;
using Netcorext.Algorithms;
using Netcorext.Auth.Domain.Entities;
using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.EntityFramework.UserIdentityPattern.Extensions;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role;

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
        var dsExtendData = _context.Set<RoleExtendData>();
        var dsPermission = _context.Set<Permission>();
        var dsPermissionExtendData = _context.Set<PermissionExtendData>();

        if (!await ds.AnyAsync(t => t.Id == request.Id, cancellationToken)) return Result.NotFound;

        var entity = ds.Include(t => t.ExtendData)
                       .Include(t => t.Permissions).ThenInclude(t => t.ExtendData)
                       .First(t => t.Id == request.Id);

        _context.Entry(entity).UpdateProperty(t => t.Name, request.Name);
        _context.Entry(entity).UpdateProperty(t => t.Priority, request.Priority);
        _context.Entry(entity).UpdateProperty(t => t.Disabled, request.Disabled);

        if (request.ExtendData != null && request.ExtendData.Any())
        {
            // 先將請求資料依照 CRUD 進行分組
            var gExtendData = request.ExtendData
                                     .GroupBy(t => t.CRUD, (mode, data) => new
                                                                           {
                                                                               Mode = mode,
                                                                               Data = data.Select(t => new RoleExtendData
                                                                                                       {
                                                                                                           Id = entity.Id,
                                                                                                           Key = t.Key,
                                                                                                           Value = t.Value
                                                                                                       })
                                                                                          .ToArray()
                                                                           })
                                     .ToArray();

            var createExtendData = gExtendData.FirstOrDefault(t => t.Mode == CRUD.C)?.Data ?? Array.Empty<RoleExtendData>();
            var updateExtendData = gExtendData.FirstOrDefault(t => t.Mode == CRUD.U)?.Data ?? Array.Empty<RoleExtendData>();
            var deleteExtendData = gExtendData.FirstOrDefault(t => t.Mode == CRUD.D)?.Data ?? Array.Empty<RoleExtendData>();

            var extendData = entity.ExtendData
                                   .Join(deleteExtendData, t => new { t.Id, t.Key }, t => new { t.Id, t.Key }, (src, desc) => src)
                                   .ToArray();

            if (extendData.Any()) dsExtendData.RemoveRange(extendData);

            if (createExtendData.Any()) dsExtendData.AddRange(createExtendData);

            extendData = entity.ExtendData
                               .Join(updateExtendData, t => new { t.Id, t.Key }, t => new { t.Id, t.Key },
                                     (src, desc) =>
                                     {
                                         src.Value = desc.Value;

                                         return src;
                                     })
                               .ToArray();

            dsExtendData.UpdateRange(extendData);
        }

        if (request.Permissions != null && request.Permissions.Any())
        {
            // 先將請求資料依照 CRUD 進行分組
            var gPermissions = request.Permissions
                                      .GroupBy(t => t.CRUD, (mode, data) => new
                                                                            {
                                                                                Mode = mode,
                                                                                Data = data.Select(t =>
                                                                                                   {
                                                                                                       var pid = t.CRUD == CRUD.C ? _snowflake.Generate() : t.Id ?? 0;

                                                                                                       return new
                                                                                                              {
                                                                                                                  Permission = new Permission
                                                                                                                               {
                                                                                                                                   Id = pid,
                                                                                                                                   FunctionId = t.FunctionId!,
                                                                                                                                   PermissionType = t.PermissionType!.Value,
                                                                                                                                   Allowed = t.Allowed!.Value,
                                                                                                                                   Priority = t.Priority!.Value,
                                                                                                                                   ReplaceExtendData = t.ReplaceExtendData!.Value,
                                                                                                                                   ExpireDate = t.ExpireDate
                                                                                                                               },
                                                                                                                  ExtendData = (t.ExtendData ?? Array.Empty<UpdateRole.PermissionExtendData>())
                                                                                                                              .GroupBy(t2 => t2.CRUD,
                                                                                                                                       (mode2, data2) => new
                                                                                                                                                         {
                                                                                                                                                             Mode = mode2,
                                                                                                                                                             Data = data2.Select(t3 => new PermissionExtendData
                                                                                                                                                                                       {
                                                                                                                                                                                           Id = pid,
                                                                                                                                                                                           Key = t3.Key,
                                                                                                                                                                                           Value = t3.Value,
                                                                                                                                                                                           PermissionType = t3.PermissionType,
                                                                                                                                                                                           Allowed = t3.Allowed
                                                                                                                                                                                       })
                                                                                                                                                                         .ToArray()
                                                                                                                                                         })
                                                                                                                              .ToArray()
                                                                                                              };
                                                                                                   })
                                                                                           .ToArray()
                                                                            })
                                      .ToArray();

            var createPermission = gPermissions.FirstOrDefault(t => t.Mode == CRUD.C)?.Data;
            var updatePermission = gPermissions.FirstOrDefault(t => t.Mode == CRUD.U)?.Data;
            var deletePermission = gPermissions.FirstOrDefault(t => t.Mode == CRUD.D)?.Data;

            if (deletePermission != null && deletePermission.Any())
            {
                var permissions = entity.Permissions
                                        .Join(deletePermission.Select(t => t.Permission), t => t.Id, t => t.Id, (src, desc) => src)
                                        .ToArray();

                var permissionExtendData = permissions.SelectMany(t => t.ExtendData)
                                                      .ToArray();

                if (permissionExtendData.Any()) dsPermissionExtendData.RemoveRange(permissionExtendData);

                if (permissions.Any()) dsPermission.RemoveRange(permissions);
            }

            if (createPermission != null && createPermission.Any())
            {
                if (createPermission.Any())
                    dsPermission.AddRange(createPermission.Select(t =>
                                                                  {
                                                                      t.Permission.ExtendData = t.ExtendData.FirstOrDefault(t2 => t2.Mode == CRUD.C)?.Data ?? Array.Empty<PermissionExtendData>();

                                                                      return t.Permission;
                                                                  }));
            }

            var createPermissionExtendData = updatePermission?.SelectMany(t => t.ExtendData).FirstOrDefault(t => t.Mode == CRUD.C)?.Data ?? Array.Empty<PermissionExtendData>();
            var updatePermissionExtendData = updatePermission?.SelectMany(t => t.ExtendData).FirstOrDefault(t => t.Mode == CRUD.U)?.Data ?? Array.Empty<PermissionExtendData>();
            var deletePermissionExtendData = updatePermission?.SelectMany(t => t.ExtendData).FirstOrDefault(t => t.Mode == CRUD.D)?.Data ?? Array.Empty<PermissionExtendData>();

            if (deletePermissionExtendData.Any())
            {
                var permissionExtendData = entity.Permissions
                                                 .SelectMany(t => t.ExtendData)
                                                 .Join(deletePermissionExtendData, t => new { t.Id, t.Key }, t => new { t.Id, t.Key }, (src, desc) => src)
                                                 .ToArray();

                if (permissionExtendData.Any()) dsPermissionExtendData.RemoveRange(permissionExtendData);
            }

            if (createPermissionExtendData.Any()) dsPermissionExtendData.AddRange(createPermissionExtendData);

            if (updatePermissionExtendData.Any())
            {
                var permissionExtendData = entity.Permissions
                                                 .SelectMany(t => t.ExtendData)
                                                 .Join(updatePermissionExtendData, t => new { t.Id, t.Key }, t => new { t.Id, t.Key },
                                                       (src, desc) =>
                                                       {
                                                           src.Value = desc.Value;
                                                           src.PermissionType = desc.PermissionType;
                                                           src.Allowed = desc.Allowed;

                                                           return src;
                                                       })
                                                 .ToArray();

                if (permissionExtendData.Any()) dsPermissionExtendData.UpdateRange(permissionExtendData);
            }
        }


        await _context.SaveChangesAsync(cancellationToken);

        return Result.SuccessNoContent;
    }
}
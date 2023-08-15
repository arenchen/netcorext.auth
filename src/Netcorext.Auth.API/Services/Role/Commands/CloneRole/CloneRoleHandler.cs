using Microsoft.EntityFrameworkCore;
using Netcorext.Algorithms;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role.Commands;

public class CloneRoleHandler : IRequestHandler<CloneRole, Result<long?>>
{
    private readonly DatabaseContext _context;
    private readonly ISnowflake _snowflake;

    public CloneRoleHandler(DatabaseContextAdapter context, ISnowflake snowflake)
    {
        _context = context;
        _snowflake = snowflake;
    }

    public async Task<Result<long?>> Handle(CloneRole request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Role>();

        if (!await ds.AnyAsync(t => t.Id == request.SourceId, cancellationToken))
            return Result<long?>.DependencyNotFound;

        if (await ds.AnyAsync(t => t.Name.ToUpper() == request.Name.ToUpper(), cancellationToken))
            return Result<long?>.Conflict;

        var entity = ds.Include(t => t.ExtendData)
                       .Include(t => t.Permissions)
                       .Include(t => t.PermissionConditions)
                       .AsNoTracking()
                       .First(t => t.Id == request.SourceId);

        entity.Id = request.CustomId ?? _snowflake.Generate();
        entity.Name = request.Name;
        entity.Disabled = request.Disabled;
        entity.ExtendData.ForEach(t => t.Id = entity.Id);
        entity.Permissions.ForEach(t => t.Id = entity.Id);

        entity.PermissionConditions.ForEach(t =>
                                            {
                                                t.Id = _snowflake.Generate();
                                                t.RoleId = entity.Id;
                                            });

        if (request.ExtendData?.Any() == true)
        {
            entity.ExtendData = request.ExtendData
                                       .Select(t => new Domain.Entities.RoleExtendData
                                                    {
                                                        Id = entity.Id,
                                                        Key = t.Key.ToUpper(),
                                                        Value = t.Value.ToUpper()
                                                    })
                                       .Union(entity.ExtendData)
                                       .DistinctBy(t => t.Key)
                                       .ToArray();
        }

        if (request.Permissions?.Any() == true)
        {
            entity.Permissions = request.Permissions.Select(t => new Domain.Entities.RolePermission
                                                                 {
                                                                     Id = entity.Id,
                                                                     PermissionId = t.PermissionId
                                                                 })
                                        .Union(entity.Permissions)
                                        .DistinctBy(t => t.PermissionId)
                                        .ToArray();
        }

        if (request.DefaultPermissionConditions?.Any() == true)
        {
            var defaultPermissionConditions = new List<CloneRole.RolePermissionCondition>();

            foreach (var permission in entity.Permissions)
            {
                defaultPermissionConditions.AddRange(request.DefaultPermissionConditions.Select(t => new CloneRole.RolePermissionCondition
                                                                                                     {
                                                                                                         PermissionId = permission.PermissionId,
                                                                                                         Group = t.Group?.ToUpper(),
                                                                                                         Key = t.Key.ToUpper(),
                                                                                                         Value = t.Value.ToUpper()
                                                                                                     }));
            }

            var permissionConditions = request.PermissionConditions ?? Array.Empty<CloneRole.RolePermissionCondition>();

            request.PermissionConditions = permissionConditions.Union(defaultPermissionConditions)
                                                               .DistinctBy(t => new { t.PermissionId, t.Group, t.Key, t.Value })
                                                               .ToArray();
        }

        if (request.PermissionConditions?.Any() == true)
        {
            entity.PermissionConditions = request.PermissionConditions.Select(t => new Domain.Entities.RolePermissionCondition
                                                                                   {
                                                                                       Id = _snowflake.Generate(),
                                                                                       RoleId = entity.Id,
                                                                                       PermissionId = t.PermissionId,
                                                                                       Group = t.Group?.ToUpper(),
                                                                                       Key = t.Key.ToUpper(),
                                                                                       Value = t.Value.ToUpper()
                                                                                   })
                                                 .Union(entity.PermissionConditions)
                                                 .DistinctBy(t => new { t.PermissionId, t.Group, t.Key, t.Value })
                                                 .ToArray();
        }

        await ds.AddAsync(entity, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<long?>.SuccessCreated.Clone(entity.Id);
    }
}
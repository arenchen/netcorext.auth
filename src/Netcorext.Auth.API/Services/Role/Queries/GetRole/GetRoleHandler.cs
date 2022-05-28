using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role;

public class GetRoleHandler : IRequestHandler<GetRole, Result<IEnumerable<Models.Role>>>
{
    private readonly DatabaseContext _context;

    public GetRoleHandler(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Result<IEnumerable<Models.Role>>> Handle(GetRole request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Role>();

        Expression<Func<Domain.Entities.Role, bool>> predicate = p => true;

        if (request.Id.HasValue) predicate = predicate.And(p => p.Id == request.Id.Value);

        if (!request.Name.IsEmpty()) predicate = predicate.And(p => p.Name.Contains(request.Name));
        if (!request.Priority.IsEmpty()) predicate = predicate.And(p => p.Priority == request.Priority);
        if (request.Disabled.HasValue) predicate = predicate.And(p => p.Disabled == request.Disabled);

        if (request.ExtendData != null && request.ExtendData.Any())
        {
            predicate = request.ExtendData.Aggregate(predicate, (expression, extendData) => expression.And(t => t.ExtendData.Any(t2 => t2.Key == extendData.Key && t2.Value == extendData.Value)));
        }

        if (request.Permission != null)
        {
            Expression<Func<Domain.Entities.Permission, bool>> predicatePermission = p => true;

            if (!request.Permission.FunctionId.IsEmpty()) predicatePermission = predicatePermission.And(p => p.FunctionId == request.Permission.FunctionId);
            if (request.Permission.PermissionType.HasValue) predicatePermission = predicatePermission.And(p => p.PermissionType == request.Permission.PermissionType);
            if (request.Permission.Allowed.HasValue) predicatePermission = predicatePermission.And(p => p.Allowed == request.Permission.Allowed);
            if (request.Permission.Priority.HasValue) predicatePermission = predicatePermission.And(p => p.Priority == request.Permission.Priority);
            if (request.Permission.ReplaceExtendData.HasValue) predicatePermission = predicatePermission.And(p => p.ReplaceExtendData == request.Permission.ReplaceExtendData);
            if (request.Permission.ExpireDate.HasValue) predicatePermission = predicatePermission.And(p => p.ExpireDate == request.Permission.ExpireDate);

            if (request.Permission.ExtendData != null && request.Permission.ExtendData.Any())
            {
                Expression<Func<Domain.Entities.PermissionExtendData, bool>> predicatePermissionExtendData = p => true;

                predicatePermissionExtendData = request.Permission.ExtendData.Aggregate(predicatePermissionExtendData, (expression, extendData) =>
                                                                                                                       {
                                                                                                                           if (!extendData.Key.IsEmpty()) expression = expression.And(t => t.Key == extendData.Key);
                                                                                                                           if (!extendData.Value.IsEmpty()) expression = expression.And(t => t.Value == extendData.Value);
                                                                                                                           if (extendData.PermissionType.HasValue) expression = expression.And(t => t.PermissionType == extendData.PermissionType);
                                                                                                                           if (extendData.Allowed.HasValue) expression = expression.And(t => t.Allowed == extendData.Allowed);

                                                                                                                           return expression;
                                                                                                                       });

                predicatePermission = predicatePermission.And(t => t.ExtendData.Any(predicatePermissionExtendData.Compile()));
            }

            predicate = predicate.And(t => t.Permissions.Any(predicatePermission.Compile()));
        }

        var queryEntities = ds.Include(t => t.ExtendData)
                              .Include(t => t.Permissions).ThenInclude(t => t.ExtendData)
                              .Where(predicate)
                              .AsNoTracking();

        var pagination = await queryEntities.GroupBy(t => 0)
                                            .Select(t => new
                                                         {
                                                             Count = t.Count(),
                                                             Rows = t.OrderBy(t2 => t2.Id)
                                                                     .Skip(request.Paging.Offset)
                                                                     .Take(request.Paging.Limit)
                                                         })
                                            .FirstOrDefaultAsync(cancellationToken);

        request.Paging.Count = pagination?.Count ?? 0;

        var content = pagination?.Rows.Select(t => new Models.Role
                                                   {
                                                       Id = t.Id,
                                                       Name = t.Name,
                                                       Priority = t.Priority,
                                                       Disabled = t.Disabled,
                                                       ExtendData = t.ExtendData.Select(t2 => new Models.RoleExtendData
                                                                                              {
                                                                                                  Key = t2.Key,
                                                                                                  Value = t2.Value,
                                                                                                  CreationDate = t2.CreationDate,
                                                                                                  CreatorId = t2.CreatorId,
                                                                                                  ModificationDate = t2.ModificationDate,
                                                                                                  ModifierId = t2.ModifierId
                                                                                              }),
                                                       Permissions = t.Permissions.Select(t2 => new Models.Permission
                                                                                                {
                                                                                                    Id = t2.Id,
                                                                                                    FunctionId = t2.FunctionId,
                                                                                                    PermissionType = t2.PermissionType,
                                                                                                    Allowed = t2.Allowed,
                                                                                                    Priority = t2.Priority,
                                                                                                    ReplaceExtendData = t2.ReplaceExtendData,
                                                                                                    ExpireDate = t2.ExpireDate,
                                                                                                    ExtendData = t2.ExtendData.Select(t3 => new Models.PermissionExtendData
                                                                                                                                            {
                                                                                                                                                Key = t3.Key,
                                                                                                                                                Value = t3.Value,
                                                                                                                                                PermissionType = t3.PermissionType,
                                                                                                                                                Allowed = t3.Allowed
                                                                                                                                            }),
                                                                                                    CreationDate = t2.CreationDate,
                                                                                                    CreatorId = t2.CreatorId,
                                                                                                    ModificationDate = t2.ModificationDate,
                                                                                                    ModifierId = t2.ModifierId
                                                                                                }),
                                                       CreationDate = t.CreationDate,
                                                       CreatorId = t.CreatorId,
                                                       ModificationDate = t.ModificationDate,
                                                       ModifierId = t.ModifierId
                                                   })
                                 .ToArray();

        if (content != null && !content.Any()) content = null;

        return Result<IEnumerable<Models.Role>>.Success.Clone(content, request.Paging);
    }
}
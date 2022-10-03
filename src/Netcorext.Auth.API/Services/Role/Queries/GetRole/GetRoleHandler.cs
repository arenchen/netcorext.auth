using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role.Queries;

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
        if (request.Disabled.HasValue) predicate = predicate.And(p => p.Disabled == request.Disabled);

        if (request.ExtendData != null && request.ExtendData.Any())
        {
            predicate = request.ExtendData.Aggregate(predicate, (expression, extendData) => expression.And(t => t.ExtendData.Any(t2 => t2.Key == extendData.Key && t2.Value == extendData.Value)));
        }

        var queryEntities = ds.Include(t => t.ExtendData)
                              .Include(t => t.Permissions).ThenInclude(t => t.Permission).ThenInclude(t => t.Rules)
                              .Include(t => t.PermissionConditions).ThenInclude(t => t.Permission)
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
                                                       Permissions = t.Permissions.Select(t2 => new Models.RolePermission
                                                                                                {
                                                                                                    PermissionId = t2.Id,
                                                                                                    Name = t2.Permission.Name,
                                                                                                    CreationDate = t2.CreationDate,
                                                                                                    CreatorId = t2.CreatorId,
                                                                                                    ModificationDate = t2.ModificationDate,
                                                                                                    ModifierId = t2.ModifierId
                                                                                                }),
                                                       PermissionConditions = t.PermissionConditions.Select(t2 => new Models.RolePermissionCondition
                                                                                                                  {
                                                                                                                      Id = t2.Id,
                                                                                                                      PermissionId = t2.PermissionId,
                                                                                                                      Priority = t2.Priority,
                                                                                                                      Key = t2.Key,
                                                                                                                      Value = t2.Value,
                                                                                                                      Allowed = t2.Allowed,
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
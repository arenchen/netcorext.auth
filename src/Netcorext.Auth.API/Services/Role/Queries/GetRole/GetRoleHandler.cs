using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Settings;
using Netcorext.Configuration.Extensions;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role.Queries;

public class GetRoleHandler : IRequestHandler<GetRole, Result<IEnumerable<Models.Role>>>
{
    private readonly DatabaseContext _context;
    private readonly int _dataSizeLimit;

    public GetRoleHandler(DatabaseContext context, IOptions<ConfigSettings> config)
    {
        _context = context;
        _dataSizeLimit = config.Value.Connections.RelationalDb.GetDefault().DataSizeLimit;
    }

    public async Task<Result<IEnumerable<Models.Role>>> Handle(GetRole request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Role>();

        Expression<Func<Domain.Entities.Role, bool>> predicate = p => true;

        if (request.Ids?.Any() == true) predicate = predicate.And(p => request.Ids.Contains(p.Id));
        if (!request.Name.IsEmpty()) predicate = predicate.And(p => p.Name.Contains(request.Name));
        if (request.Disabled.HasValue) predicate = predicate.And(p => p.Disabled == request.Disabled);

        if (request.ExtendData != null && request.ExtendData.Any())
        {
            predicate = request.ExtendData.Aggregate(predicate, (expression, extendData) => expression.And(t => t.ExtendData.Any(t2 => t2.Key == extendData.Key && t2.Value == extendData.Value)));
        }

        var queryEntities = ds.Where(predicate)
                              .OrderBy(t => t.Id)
                              .Take(_dataSizeLimit)
                              .AsNoTracking();

        var pagination = await queryEntities.GroupBy(t => 0)
                                            .Select(t => new
                                                         {
                                                             Count = t.Count(),
                                                             Rows = t.OrderBy(t2 => t2.Id)
                                                                     .Skip(request.Paging.Offset)
                                                                     .Take(request.Paging.Limit)
                                                                     .Select(t2 => new Models.Role
                                                                                   {
                                                                                       Id = t2.Id,
                                                                                       Name = t2.Name,
                                                                                       Disabled = t2.Disabled,
                                                                                       ExtendData = t2.ExtendData.Select(t3 => new Models.RoleExtendData
                                                                                                                               {
                                                                                                                                   Key = t3.Key,
                                                                                                                                   Value = t3.Value,
                                                                                                                                   CreationDate = t3.CreationDate,
                                                                                                                                   CreatorId = t3.CreatorId,
                                                                                                                                   ModificationDate = t3.ModificationDate,
                                                                                                                                   ModifierId = t3.ModifierId
                                                                                                                               }),
                                                                                       Permissions = t2.Permissions.Select(t3 => new Models.RolePermission
                                                                                                                                 {
                                                                                                                                     PermissionId = t3.PermissionId,
                                                                                                                                     Name = t3.Permission.Name,
                                                                                                                                     CreationDate = t3.CreationDate,
                                                                                                                                     CreatorId = t3.CreatorId,
                                                                                                                                     ModificationDate = t3.ModificationDate,
                                                                                                                                     ModifierId = t3.ModifierId
                                                                                                                                 }),
                                                                                       PermissionConditions = t2.PermissionConditions.Select(t3 => new Models.RolePermissionCondition
                                                                                                                                                   {
                                                                                                                                                       Id = t3.Id,
                                                                                                                                                       PermissionId = t3.PermissionId,
                                                                                                                                                       Priority = t3.Priority,
                                                                                                                                                       Group = t3.Group,
                                                                                                                                                       Key = t3.Key,
                                                                                                                                                       Value = t3.Value,
                                                                                                                                                       Allowed = t3.Allowed,
                                                                                                                                                       CreationDate = t3.CreationDate,
                                                                                                                                                       CreatorId = t3.CreatorId,
                                                                                                                                                       ModificationDate = t3.ModificationDate,
                                                                                                                                                       ModifierId = t3.ModifierId
                                                                                                                                                   }),
                                                                                       CreationDate = t2.CreationDate,
                                                                                       CreatorId = t2.CreatorId,
                                                                                       ModificationDate = t2.ModificationDate,
                                                                                       ModifierId = t2.ModifierId
                                                                                   })
                                                         })
                                            .FirstOrDefaultAsync(cancellationToken);

        request.Paging.Count = pagination?.Count ?? 0;

        var content = pagination?.Rows.ToArray();

        if (content != null && !content.Any()) content = null;

        return Result<IEnumerable<Models.Role>>.Success.Clone(content, request.Paging);
    }
}
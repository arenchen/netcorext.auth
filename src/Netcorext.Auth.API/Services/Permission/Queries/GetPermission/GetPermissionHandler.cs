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

namespace Netcorext.Auth.API.Services.Permission.Queries;

public class GetPermissionHandler : IRequestHandler<GetPermission, Result<IEnumerable<Models.Permission>>>
{
    private readonly DatabaseContext _context;
    private readonly int _dataSizeLimit;

    public GetPermissionHandler(DatabaseContext context, IOptions<ConfigSettings> config)
    {
        _context = context;
        _dataSizeLimit = config.Value.Connections.RelationalDb.GetDefault().DataSizeLimit;
    }

    public async Task<Result<IEnumerable<Models.Permission>>> Handle(GetPermission request, CancellationToken cancellationToken = new())
    {
        var ds = _context.Set<Domain.Entities.Permission>();

        Expression<Func<Domain.Entities.Permission, bool>> predicate = p => true;

        if (request.Id.HasValue) predicate = predicate.And(p => p.Id == request.Id.Value);

        if (!request.Name.IsEmpty()) predicate = predicate.And(p => p.Name.ToUpper().Contains(request.Name.ToUpper()));

        if (request.Disabled.HasValue) predicate = predicate.And(p => p.Disabled == request.Disabled);

        if (request.Rule != null)
        {
            Expression<Func<Domain.Entities.Rule, bool>> predicateRule = p => true;

            if (!request.Rule.FunctionId.IsEmpty())
                predicateRule = predicateRule.And(t => t.FunctionId.ToUpper() == request.Rule.FunctionId.ToUpper());

            if (!request.Rule.PermissionTypes.IsEmpty() && request.Rule.PermissionTypes.Any())
                predicateRule = predicateRule.And(t => request.Rule.PermissionTypes.Contains(t.PermissionType));

            if (request.Rule.Allowed.HasValue)
                predicateRule = predicateRule.And(t => t.Allowed == request.Rule.Allowed);

            predicate = predicate.And(p => p.Rules.AsQueryable().Any(predicateRule.Compile()));
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
                                                                     .Select(t2 => new Models.Permission
                                                                                   {
                                                                                       Id = t2.Id,
                                                                                       Name = t2.Name,
                                                                                       Priority = t2.Priority,
                                                                                       Disabled = t2.Disabled,
                                                                                       CreationDate = t2.CreationDate,
                                                                                       CreatorId = t2.CreatorId,
                                                                                       ModificationDate = t2.ModificationDate,
                                                                                       ModifierId = t2.ModifierId,
                                                                                       Rules = t2.Rules.Select(t3 => new Models.Rule
                                                                                                                     {
                                                                                                                         Id = t3.Id,
                                                                                                                         FunctionId = t3.FunctionId,
                                                                                                                         PermissionType = t3.PermissionType,
                                                                                                                         Allowed = t3.Allowed,
                                                                                                                         CreationDate = t3.CreationDate,
                                                                                                                         CreatorId = t3.CreatorId,
                                                                                                                         ModificationDate = t3.ModificationDate,
                                                                                                                         ModifierId = t3.ModifierId
                                                                                                                     })
                                                                                   }
                                                                            )
                                                         })
                                            .FirstOrDefaultAsync(cancellationToken);

        request.Paging.Count = pagination?.Count ?? 0;

        var content = pagination?.Rows.ToArray();

        if (content != null && !content.Any()) content = null;

        return Result<IEnumerable<Models.Permission>>.Success.Clone(content, request.Paging);
    }
}
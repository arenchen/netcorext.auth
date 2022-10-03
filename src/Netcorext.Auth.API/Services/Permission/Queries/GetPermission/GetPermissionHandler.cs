using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Permission.Queries;

public class GetPermissionHandler : IRequestHandler<GetPermission, Result<IEnumerable<Models.Permission>>>
{
    private readonly DatabaseContext _context;

    public GetPermissionHandler(DatabaseContext context)
    {
        _context = context;
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

            predicate = predicate.And(p => p.Rules.Any(predicateRule.Compile()));
        }

        var queryEntities = ds.Include(t => t.Rules)
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

        var content = pagination?.Rows.Select(t => new Models.Permission
                                                   {
                                                       Id = t.Id,
                                                       Name = t.Name,
                                                       Disabled = t.Disabled,
                                                       CreationDate = t.CreationDate,
                                                       CreatorId = t.CreatorId,
                                                       ModificationDate = t.ModificationDate,
                                                       ModifierId = t.ModifierId,
                                                       Rules = t.Rules.Select(t2 => new Models.Rule
                                                                                    {
                                                                                        Id = t2.Id,
                                                                                        FunctionId = t2.FunctionId,
                                                                                        PermissionType = t2.PermissionType,
                                                                                        Allowed = t2.Allowed,
                                                                                        CreationDate = t2.CreationDate,
                                                                                        CreatorId = t2.CreatorId,
                                                                                        ModificationDate = t2.ModificationDate,
                                                                                        ModifierId = t2.ModifierId
                                                                                    })
                                                   }
                                             )
                                 .ToArray();

        if (content != null && !content.Any()) content = null;

        return Result<IEnumerable<Models.Permission>>.Success.Clone(content, request.Paging);
    }
}
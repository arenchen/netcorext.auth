using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Client;

public class GetClientHandler : IRequestHandler<GetClient, Result<IEnumerable<Models.Client>>>
{
    private readonly DatabaseContext _context;

    public GetClientHandler(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Result<IEnumerable<Models.Client>>> Handle(GetClient request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Client>();

        Expression<Func<Domain.Entities.Client, bool>> predicate = p => true;

        if (request.Id.HasValue) predicate = predicate.And(p => p.Id == request.Id.Value);

        if (!request.Name.IsEmpty()) predicate = predicate.And(p => p.Name.Contains(request.Name));

        if (!request.CallbackUrl.IsEmpty()) predicate = predicate.And(p => p.CallbackUrl!.Contains(request.CallbackUrl));

        if (request.Disabled.HasValue) predicate = predicate.And(p => p.Disabled == request.Disabled);

        if (request.Role != null)
        {
            Expression<Func<Domain.Entities.ClientRole, bool>> predicateRole = p => !p.Role.Disabled;

            if (request.Role.RoleId.HasValue) predicateRole = predicateRole.And(p => p.RoleId == request.Role.RoleId);
            if (!request.Role.Name.IsEmpty()) predicateRole = predicateRole.And(p => p.Role.Name.Contains(request.Role.Name));
            if (request.Role.Priority.HasValue) predicateRole = predicateRole.And(p => p.Role.Priority == request.Role.Priority);
            if (request.Role.ExpireDate.HasValue) predicateRole = predicateRole.And(p => p.ExpireDate == request.Role.ExpireDate);

            predicate = predicate.And(t => t.Roles.Any(predicateRole.Compile()));
        }

        if (request.ExtendData != null && request.ExtendData.Any())
        {
            predicate = request.ExtendData.Aggregate(predicate, (expression, extendData) => expression.And(t => t.ExtendData.Any(t2 => t2.Key == extendData.Key.ToUpper() && t2.Value == extendData.Value)));
        }

        var queryEntities = ds.Include(t => t.ExtendData)
                              .Include(t => t.Roles).ThenInclude(t => t.Role)
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

        var content = pagination?.Rows.Select(t => new Models.Client
                                                   {
                                                       Id = t.Id,
                                                       Name = t.Name,
                                                       CallbackUrl = t.CallbackUrl,
                                                       TokenExpireSeconds = t.TokenExpireSeconds,
                                                       RefreshTokenExpireSeconds = t.RefreshTokenExpireSeconds,
                                                       CodeExpireSeconds = t.CodeExpireSeconds,
                                                       Disabled = t.Disabled,
                                                       CreationDate = t.CreationDate,
                                                       CreatorId = t.CreatorId,
                                                       ModificationDate = t.ModificationDate,
                                                       ModifierId = t.ModifierId,
                                                       Roles = t.Roles.Select(t2 => new Models.ClientRole
                                                                                    {
                                                                                        RoleId = t2.RoleId,
                                                                                        Name = t2.Role.Name,
                                                                                        Priority = t2.Role.Priority,
                                                                                        ExpireDate = t2.ExpireDate,
                                                                                        CreationDate = t2.CreationDate,
                                                                                        CreatorId = t2.CreatorId,
                                                                                        ModificationDate = t2.ModificationDate,
                                                                                        ModifierId = t2.ModifierId
                                                                                    }),
                                                       ExtendData = t.ExtendData.Select(t2 => new Models.ClientExtendData
                                                                                              {
                                                                                                  Key = t2.Key,
                                                                                                  Value = t2.Value,
                                                                                                  CreationDate = t2.CreationDate,
                                                                                                  CreatorId = t2.CreatorId,
                                                                                                  ModificationDate = t2.ModificationDate,
                                                                                                  ModifierId = t2.ModifierId
                                                                                              })
                                                   }
                                             )
                                 .ToArray();

        if (content != null && !content.Any()) content = null;

        return Result<IEnumerable<Models.Client>>.Success.Clone(content, request.Paging);
    }
}
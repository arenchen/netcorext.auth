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

namespace Netcorext.Auth.API.Services.Client.Queries;

public class GetClientHandler : IRequestHandler<GetClient, Result<IEnumerable<Models.Client>>>
{
    private readonly DatabaseContext _context;
    private readonly int _dataSizeLimit;

    public GetClientHandler(DatabaseContextAdapter context, IOptions<ConfigSettings> config)
    {
        _context = context.Slave;
        _dataSizeLimit = config.Value.Connections.RelationalDb.GetDefault().DataSizeLimit;
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
            if (request.Role.ExpireDate.HasValue) predicateRole = predicateRole.And(p => p.ExpireDate == request.Role.ExpireDate);

            predicate = predicate.And(t => t.Roles.AsQueryable().Any(predicateRole.Compile()));
        }

        if (request.ExtendData != null && request.ExtendData.Any())
        {
            Expression<Func<Domain.Entities.ClientExtendData, bool>> predicateExtendData = p => false;

            var extendData = request.ExtendData.GroupBy(t => t.Key, (k, values) =>
                                                                        new
                                                                        {
                                                                            Key = k.ToUpper(),
                                                                            Values = values.Select(t => t.Value.ToUpper())
                                                                        });

            predicateExtendData = extendData.Aggregate(predicateExtendData, (current, item) => current.Or(t => t.Key == item.Key && item.Values.Contains(t.Value)));

            predicate = predicate.And(t => t.ExtendData.AsQueryable().Any(predicateExtendData));
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
                                                                     .Select(t2 => new Models.Client
                                                                                   {
                                                                                       Id = t2.Id,
                                                                                       Name = t2.Name,
                                                                                       CallbackUrl = t2.CallbackUrl,
                                                                                       AllowedRefreshToken = t2.AllowedRefreshToken,
                                                                                       TokenExpireSeconds = t2.TokenExpireSeconds,
                                                                                       RefreshTokenExpireSeconds = t2.RefreshTokenExpireSeconds,
                                                                                       CodeExpireSeconds = t2.CodeExpireSeconds,
                                                                                       Disabled = t2.Disabled,
                                                                                       CreationDate = t2.CreationDate,
                                                                                       CreatorId = t2.CreatorId,
                                                                                       ModificationDate = t2.ModificationDate,
                                                                                       ModifierId = t2.ModifierId,
                                                                                       Roles = t2.Roles.Select(t2 => new Models.ClientRole
                                                                                                                     {
                                                                                                                         RoleId = t2.RoleId,
                                                                                                                         Name = t2.Role.Name,
                                                                                                                         ExpireDate = t2.ExpireDate,
                                                                                                                         CreationDate = t2.CreationDate,
                                                                                                                         CreatorId = t2.CreatorId,
                                                                                                                         ModificationDate = t2.ModificationDate,
                                                                                                                         ModifierId = t2.ModifierId
                                                                                                                     }),
                                                                                       ExtendData = t2.ExtendData.Select(t3 => new Models.ClientExtendData
                                                                                                                               {
                                                                                                                                   Key = t3.Key,
                                                                                                                                   Value = t3.Value,
                                                                                                                                   CreationDate = t3.CreationDate,
                                                                                                                                   CreatorId = t3.CreatorId,
                                                                                                                                   ModificationDate = t3.ModificationDate,
                                                                                                                                   ModifierId = t3.ModifierId
                                                                                                                               })
                                                                                   }
                                                                            )
                                                         })
                                            .SingleOrDefaultAsync(cancellationToken);

        request.Paging.Count = pagination?.Count ?? 0;

        var content = pagination?.Rows.ToArray();

        if (content != null && !content.Any()) content = null;

        return Result<IEnumerable<Models.Client>>.Success.Clone(content, request.Paging);
    }
}
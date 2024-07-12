using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Settings;
using Netcorext.Configuration.Extensions;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserRoleHandler : IRequestHandler<GetUserRole, Result<IEnumerable<Models.FullUserRole>>>
{
    private readonly DatabaseContext _context;
    private readonly int _dataSizeLimit;

    public GetUserRoleHandler(DatabaseContextAdapter context, IOptions<ConfigSettings> config)
    {
        _context = context.Slave;
        _dataSizeLimit = config.Value.Connections.RelationalDb.GetDefault().DataSizeLimit;
    }

    public async Task<Result<IEnumerable<Models.FullUserRole>>> Handle(GetUserRole request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.UserRole>();

        Expression<Func<Domain.Entities.UserRole, bool>> predicate = t => request.Ids.Contains(t.Id) && !t.Role.Disabled;

        if (!request.IncludeExpired)
            predicate = predicate.And(t => t.ExpireDate > DateTime.UtcNow);

        var queryEntities = ds.Where(predicate)
                              .OrderBy(t => t.Id)
                              .Take(_dataSizeLimit)
                              .AsNoTracking();

        var content = queryEntities.Select(t => new Models.FullUserRole
                                                {
                                                    Id = t.Id,
                                                    RoleId = t.RoleId,
                                                    Name = t.Role.Name,
                                                    Priority = t.Role.Priority,
                                                    ExpireDate = t.ExpireDate,
                                                    ExtendData = request.IncludeExtendData
                                                                     ? t.Role.ExtendData.Select(t2 => new Models.RoleExtendData
                                                                                                      {
                                                                                                          Key = t2.Key,
                                                                                                          Value = t2.Value,
                                                                                                          CreationDate = t2.CreationDate,
                                                                                                          CreatorId = t2.CreatorId,
                                                                                                          ModificationDate = t2.ModificationDate,
                                                                                                          ModifierId = t2.ModifierId
                                                                                                      })
                                                                     : null,
                                                    Permissions = request.IncludePermission
                                                                      ? t.Role.Permissions.Select(t2 => new Models.RolePermission
                                                                                                        {
                                                                                                            RoleId = t2.Id,
                                                                                                            PermissionId = t2.PermissionId,
                                                                                                            Name = t2.Permission.Name,
                                                                                                            CreationDate = t2.CreationDate,
                                                                                                            CreatorId = t2.CreatorId,
                                                                                                            ModificationDate = t2.ModificationDate,
                                                                                                            ModifierId = t2.ModifierId
                                                                                                        })
                                                                      : null,
                                                    PermissionConditions = request.IncludePermissionCondition
                                                                               ? t.Role.PermissionConditions.Select(t2 => new Models.RolePermissionCondition
                                                                                                                          {
                                                                                                                              Id = t2.Id,
                                                                                                                              PermissionId = t2.PermissionId,
                                                                                                                              Group = t2.Group,
                                                                                                                              Key = t2.Key,
                                                                                                                              Value = t2.Value,
                                                                                                                              CreationDate = t2.CreationDate,
                                                                                                                              CreatorId = t2.CreatorId,
                                                                                                                              ModificationDate = t2.ModificationDate,
                                                                                                                              ModifierId = t2.ModifierId
                                                                                                                          })
                                                                               : null,
                                                    CreationDate = t.Role.CreationDate,
                                                    CreatorId = t.Role.CreatorId,
                                                    ModificationDate = t.Role.ModificationDate,
                                                    ModifierId = t.Role.ModifierId
                                                });

        if (!await content.AnyAsync(cancellationToken)) content = null;

        return Result<IEnumerable<Models.FullUserRole>>.Success.Clone(content?.ToArray());
    }
}

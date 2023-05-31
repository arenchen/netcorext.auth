using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Settings;
using Netcorext.Configuration.Extensions;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
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

    public async Task<Result<IEnumerable<Models.FullUserRole>>> Handle(GetUserRole request, CancellationToken cancellationToken = new())
    {
        var ds = _context.Set<Domain.Entities.UserRole>();

        var queryEntities = ds.Where(t => request.Ids.Contains(t.Id) && (t.ExpireDate == null || t.ExpireDate > DateTime.UtcNow) && !t.Role.Disabled)
                              .OrderBy(t => t.Id)
                              .Take(_dataSizeLimit)
                              .AsNoTracking();

        var content = queryEntities.Select(t => new Models.FullUserRole
                                                {
                                                        Id = t.Id,
                                                        RoleId = t.RoleId,
                                                        Name = t.Role.Name,
                                                        ExpireDate = t.ExpireDate,
                                                        ExtendData = t.Role.ExtendData.Select(t2 => new Models.RoleExtendData
                                                                                                    {
                                                                                                            Key = t2.Key,
                                                                                                            Value = t2.Value,
                                                                                                            CreationDate = t2.CreationDate,
                                                                                                            CreatorId = t2.CreatorId,
                                                                                                            ModificationDate = t2.ModificationDate,
                                                                                                            ModifierId = t2.ModifierId
                                                                                                    }),
                                                        Permissions = t.Role.Permissions.Select(t2 => new Models.RolePermission
                                                                                                      {
                                                                                                              PermissionId = t2.PermissionId,
                                                                                                              Name = t2.Permission.Name,
                                                                                                              CreationDate = t2.CreationDate,
                                                                                                              CreatorId = t2.CreatorId,
                                                                                                              ModificationDate = t2.ModificationDate,
                                                                                                              ModifierId = t2.ModifierId
                                                                                                      }),
                                                        PermissionConditions = t.Role.PermissionConditions.Select(t2 => new Models.RolePermissionCondition
                                                                                                                        {
                                                                                                                                Id = t2.Id,
                                                                                                                                PermissionId = t2.PermissionId,
                                                                                                                                Priority = t2.Priority,
                                                                                                                                Group = t2.Group,
                                                                                                                                Key = t2.Key,
                                                                                                                                Value = t2.Value,
                                                                                                                                Allowed = t2.Allowed,
                                                                                                                                CreationDate = t2.CreationDate,
                                                                                                                                CreatorId = t2.CreatorId,
                                                                                                                                ModificationDate = t2.ModificationDate,
                                                                                                                                ModifierId = t2.ModifierId
                                                                                                                        }),
                                                        CreationDate = t.Role.CreationDate,
                                                        CreatorId = t.Role.CreatorId,
                                                        ModificationDate = t.Role.ModificationDate,
                                                        ModifierId = t.Role.ModifierId
                                                });

        if (!await content.AnyAsync(cancellationToken)) content = null;

        return Result<IEnumerable<Models.FullUserRole>>.Success.Clone(content?.ToArray());
    }
}
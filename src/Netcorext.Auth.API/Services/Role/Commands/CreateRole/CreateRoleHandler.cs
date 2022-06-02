using Netcorext.Algorithms;
using Netcorext.Auth.Domain.Entities;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role;

public class CreateRoleHandler : IRequestHandler<CreateRole, Result<IEnumerable<long>>>
{
    private readonly DatabaseContext _context;
    private readonly ISnowflake _snowflake;

    public CreateRoleHandler(DatabaseContext context, ISnowflake snowflake)
    {
        _context = context;
        _snowflake = snowflake;
    }

    public async Task<Result<IEnumerable<long>>> Handle(CreateRole request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Role>();

        var entities = request.Roles.Select(t =>
                                            {
                                                var id = _snowflake.Generate();

                                                return new Domain.Entities.Role
                                                       {
                                                           Id = id,
                                                           Name = t.Name,
                                                           Priority = t.Priority,
                                                           Disabled = t.Disabled,
                                                           ExtendData = t.ExtendData?
                                                                         .Select(t2 => new RoleExtendData
                                                                                       {
                                                                                           Id = id,
                                                                                           Key = t2.Key,
                                                                                           Value = t2.Value
                                                                                       })
                                                                         .ToArray() ?? Array.Empty<RoleExtendData>(),
                                                           Permissions = t.Permissions?
                                                                          .Select(t2 =>
                                                                                  {
                                                                                      var pid = _snowflake.Generate();

                                                                                      return new Permission
                                                                                             {
                                                                                                 Id = pid,
                                                                                                 RoleId = id,
                                                                                                 FunctionId = t2.FunctionId,
                                                                                                 PermissionType = t2.PermissionType,
                                                                                                 Allowed = t2.Allowed,
                                                                                                 Priority = t2.Priority,
                                                                                                 ReplaceExtendData = t2.ReplaceExtendData,
                                                                                                 ExpireDate = t2.ExpireDate,
                                                                                                 ExtendData = t2.ExtendData?
                                                                                                                .Select(t3 => new PermissionExtendData
                                                                                                                              {
                                                                                                                                  Id = pid,
                                                                                                                                  Key = t3.Key,
                                                                                                                                  Value = t3.Value,
                                                                                                                                  PermissionType = t3.PermissionType,
                                                                                                                                  Allowed = t3.Allowed
                                                                                                                              })
                                                                                                                .ToArray() ?? Array.Empty<PermissionExtendData>()
                                                                                             };
                                                                                  })
                                                                          .ToArray() ?? Array.Empty<Permission>()
                                                       };
                                            })
                              .ToArray();

        await ds.AddRangeAsync(entities, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<IEnumerable<long>>.SuccessCreated.Clone(entities.Select(t => t.Id));
    }
}
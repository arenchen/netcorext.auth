using Microsoft.EntityFrameworkCore;
using Netcorext.Algorithms;
using Netcorext.Auth.Domain.Entities;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role;

public class CreateRoleHandler : IRequestHandler<CreateRole, Result<long?>>
{
    private readonly DatabaseContext _context;
    private readonly ISnowflake _snowflake;

    public CreateRoleHandler(DatabaseContext context, ISnowflake snowflake)
    {
        _context = context;
        _snowflake = snowflake;
    }

    public async Task<Result<long?>> Handle(CreateRole request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Role>();

        if (await ds.AnyAsync(t => t.Name == request.Name, cancellationToken)) return Result<long?>.Conflict;

        var id = _snowflake.Generate();

        var entity = ds.Add(new Domain.Entities.Role
                            {
                                Id = id,
                                Name = request.Name!,
                                Priority = request.Priority,
                                Disabled = request.Disabled,
                                ExtendData = request.ExtendData?
                                                    .Select(t => new RoleExtendData
                                                                 {
                                                                     Id = id,
                                                                     Key = t.Key!,
                                                                     Value = t.Value
                                                                 })
                                                    .ToArray() ?? Array.Empty<RoleExtendData>(),
                                Permissions = request.Permissions?
                                                     .Select(t =>
                                                             {
                                                                 var pid = _snowflake.Generate();
                                                                 return new Permission
                                                                        {
                                                                            Id = pid,
                                                                            RoleId = id,
                                                                            FunctionId = t.FunctionId!,
                                                                            PermissionType = t.PermissionType,
                                                                            Allowed = t.Allowed,
                                                                            Priority = t.Priority,
                                                                            ReplaceExtendData = t.ReplaceExtendData,
                                                                            ExpireDate = t.ExpireDate,
                                                                            ExtendData = t.ExtendData?
                                                                                          .Select(t2 => new PermissionExtendData
                                                                                                        {
                                                                                                            Id = pid,
                                                                                                            Key = t2.Key,
                                                                                                            Value = t2.Value,
                                                                                                            PermissionType = t2.PermissionType,
                                                                                                            Allowed = t2.Allowed
                                                                                                        })
                                                                                          .ToArray() ?? Array.Empty<PermissionExtendData>()
                                                                        };
                                                             })
                                                     .ToArray() ?? Array.Empty<Permission>()
                            });

        await _context.SaveChangesAsync(cancellationToken);

        return Result<long?>.SuccessCreated.Clone(entity.Entity.Id);
    }
}
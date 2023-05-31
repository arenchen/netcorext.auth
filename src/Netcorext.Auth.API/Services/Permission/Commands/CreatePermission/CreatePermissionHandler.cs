using Netcorext.Algorithms;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Permission.Commands;

public class CreatePermissionHandler : IRequestHandler<CreatePermission, Result<IEnumerable<long>>>
{
    private readonly DatabaseContext _context;
    private readonly ISnowflake _snowflake;

    public CreatePermissionHandler(DatabaseContextAdapter context, ISnowflake snowflake)
    {
        _context = context;
        _snowflake = snowflake;
    }

    public async Task<Result<IEnumerable<long>>> Handle(CreatePermission request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Permission>();

        if (request.Permissions.Any())
        {
            var names = request.Permissions.Select(t => t.Name.ToUpper());

            if (ds.Any(t => names.Contains(t.Name.ToUpper())))
                return Result<IEnumerable<long>>.Conflict;
        }

        var entities = request.Permissions.Select(t =>
                                                  {
                                                      var id = _snowflake.Generate();

                                                      return new Domain.Entities.Permission
                                                             {
                                                                     Id = id,
                                                                     Name = t.Name,
                                                                     Priority = t.Priority,
                                                                     Disabled = t.Disabled,
                                                                     Rules = t.Rules?.Select(t2 => new Domain.Entities.Rule
                                                                                                   {
                                                                                                           Id = _snowflake.Generate(),
                                                                                                           PermissionId = id,
                                                                                                           FunctionId = t2.FunctionId,
                                                                                                           PermissionType = t2.PermissionType,
                                                                                                           Allowed = t2.Allowed
                                                                                                   })
                                                                              .ToArray() ?? Array.Empty<Domain.Entities.Rule>()
                                                             };
                                                  })
                              .ToArray();

        await ds.AddRangeAsync(entities, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<IEnumerable<long>>.SuccessCreated.Clone(entities.Select(t => t.Id));
    }
}
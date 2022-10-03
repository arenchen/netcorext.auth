using Microsoft.EntityFrameworkCore;
using Netcorext.Algorithms;
using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.EntityFramework.UserIdentityPattern.Extensions;
using Netcorext.Extensions.Commons;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Permission.Commands;

public class UpdatePermissionHandler : IRequestHandler<UpdatePermission, Result>
{
    private readonly DatabaseContext _context;
    private readonly ISnowflake _snowflake;

    public UpdatePermissionHandler(DatabaseContext context, ISnowflake snowflake)
    {
        _context = context;
        _snowflake = snowflake;
    }

    public async Task<Result> Handle(UpdatePermission request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Permission>();
        var dsRule = _context.Set<Domain.Entities.Rule>();

        if (!await ds.AnyAsync(t => t.Id == request.Id, cancellationToken)) return Result.NotFound;
        if (!request.Name.IsEmpty() && await ds.AnyAsync(t => t.Id != request.Id && t.Name.ToUpper() == request.Name.ToUpper(), cancellationToken)) return Result.Conflict;

        var entity = ds.Include(t => t.Rules)
                       .First(t => t.Id == request.Id);

        _context.Entry(entity).UpdateProperty(t => t.Name, request.Name);
        _context.Entry(entity).UpdateProperty(t => t.Priority, request.Priority);
        _context.Entry(entity).UpdateProperty(t => t.Disabled, request.Disabled);

        if (request.Rules != null && request.Rules.Any())
        {
            var gRules = request.Rules
                                .GroupBy(t => t.Crud, (mode, roles) => new
                                                                       {
                                                                           Mode = mode,
                                                                           Data = roles.Select(t => new Domain.Entities.Rule
                                                                                                    {
                                                                                                        Id = t.Id ?? _snowflake.Generate(),
                                                                                                        PermissionId = entity.Id,
                                                                                                        FunctionId = t.FunctionId,
                                                                                                        PermissionType = t.PermissionType,
                                                                                                        Allowed = t.Allowed
                                                                                                    })
                                                                                       .ToArray()
                                                                       })
                                .ToArray();

            var createRules = gRules.FirstOrDefault(t => t.Mode == CRUD.C)?.Data ?? Array.Empty<Domain.Entities.Rule>();
            var updateRules = gRules.FirstOrDefault(t => t.Mode == CRUD.U)?.Data ?? Array.Empty<Domain.Entities.Rule>();
            var deleteRules = gRules.FirstOrDefault(t => t.Mode == CRUD.D)?.Data ?? Array.Empty<Domain.Entities.Rule>();

            var rules = entity.Rules
                              .Join(deleteRules, t => t.Id, t => t.Id, (src, desc) => src)
                              .ToArray();

            if (rules.Any()) dsRule.RemoveRange(rules);

            if (createRules.Any()) dsRule.AddRange(createRules);

            rules = entity.Rules
                          .Join(updateRules, t => t.Id, t => t.Id,
                                (src, desc) =>
                                {
                                    src.PermissionId = desc.PermissionId;
                                    src.FunctionId = desc.FunctionId;
                                    src.PermissionType = desc.PermissionType;
                                    src.Allowed = desc.Allowed;

                                    return src;
                                })
                          .ToArray();

            dsRule.UpdateRange(rules);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.SuccessNoContent;
    }
}
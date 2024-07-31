using Microsoft.EntityFrameworkCore;
using Netcorext.Algorithms;
using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.EntityFramework.UserIdentityPattern.Extensions;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Hash;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Client.Commands;

public class UpdateClientHandler : IRequestHandler<UpdateClient, Result>
{
    private readonly DatabaseContext _context;
    private readonly ISnowflake _snowflake;

    public UpdateClientHandler(DatabaseContextAdapter context, ISnowflake snowflake)
    {
        _context = context;
        _snowflake = snowflake;
    }

    public async Task<Result> Handle(UpdateClient request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Client>();
        var dsRole = _context.Set<Domain.Entities.ClientRole>();
        var dsExtendData = _context.Set<Domain.Entities.ClientExtendData>();

        var entity = await ds.Include(t => t.Roles)
                             .Include(t => t.ExtendData)
                             .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken: cancellationToken);

        if (entity == null)
            return Result.NotFound;

        if (!request.Name.IsEmpty() && await ds.AnyAsync(t => t.Id != request.Id && t.Name == request.Name, cancellationToken))
            return Result.Conflict;

        _context.Entry(entity).UpdateProperty(t => t.Name, request.Name);

        string? secret = null;
        if (!string.IsNullOrWhiteSpace(request.Secret)) secret = request.Secret.Pbkdf2HashCode(entity.CreationDate.ToUnixTimeMilliseconds());

        _context.Entry(entity).UpdateProperty(t => t.Secret, secret);
        _context.Entry(entity).UpdateProperty(t => t.CallbackUrl, request.CallbackUrl);
        _context.Entry(entity).UpdateProperty(t => t.AllowedRefreshToken, request.AllowedRefreshToken);
        _context.Entry(entity).UpdateProperty(t => t.TokenExpireSeconds, request.TokenExpireSeconds);
        _context.Entry(entity).UpdateProperty(t => t.RefreshTokenExpireSeconds, request.RefreshTokenExpireSeconds);
        _context.Entry(entity).UpdateProperty(t => t.CodeExpireSeconds, request.CodeExpireSeconds);
        _context.Entry(entity).UpdateProperty(t => t.Disabled, request.Disabled);

        if (request.Roles != null && request.Roles.Any())
        {
            var gRoles = request.Roles
                                .GroupBy(t => t.Crud, (mode, roles) => new
                                                                       {
                                                                           Mode = mode,
                                                                           Data = roles.Select(t => new Domain.Entities.ClientRole
                                                                                                    {
                                                                                                        Id = entity.Id,
                                                                                                        RoleId = t.RoleId,
                                                                                                        ExpireDate = t.ExpireDate ?? Core.Constants.MaxDateTime
                                                                                                    })
                                                                                       .ToArray()
                                                                       })
                                .ToArray();

            var createRoles = gRoles.FirstOrDefault(t => t.Mode == CRUD.C)?.Data ?? Array.Empty<Domain.Entities.ClientRole>();
            var updateRoles = gRoles.FirstOrDefault(t => t.Mode == CRUD.U)?.Data ?? Array.Empty<Domain.Entities.ClientRole>();
            var deleteRoles = gRoles.FirstOrDefault(t => t.Mode == CRUD.D)?.Data ?? Array.Empty<Domain.Entities.ClientRole>();

            var roles = entity.Roles
                              .Join(deleteRoles, t => new { t.Id, t.RoleId }, t => new { t.Id, t.RoleId }, (src, desc) => src)
                              .ToArray();

            if (roles.Any()) dsRole.RemoveRange(roles);

            if (createRoles.Any()) dsRole.AddRange(createRoles);

            roles = entity.Roles
                          .Join(updateRoles, t => new { t.Id, t.RoleId }, t => new { t.Id, t.RoleId },
                                (src, desc) =>
                                {
                                    src.ExpireDate = desc.ExpireDate;

                                    return src;
                                })
                          .ToArray();

            dsRole.UpdateRange(roles);
        }

        if (request.ExtendData != null && request.ExtendData.Any())
        {
            var gExtendData = request.ExtendData
                                     .GroupBy(t => t.Crud, (mode, extendData) => new
                                                                                 {
                                                                                     Mode = mode,
                                                                                     Data = extendData.Select(t => new Domain.Entities.ClientExtendData
                                                                                                                   {
                                                                                                                       Id = entity.Id,
                                                                                                                       Key = t.Key.ToUpper(),
                                                                                                                       Value = t.Value.ToUpper()
                                                                                                                   })
                                                                                                      .ToArray()
                                                                                 })
                                     .ToArray();

            var createExtendData = gExtendData.FirstOrDefault(t => t.Mode == CRUD.C)?.Data ?? Array.Empty<Domain.Entities.ClientExtendData>();
            var updateExtendData = gExtendData.FirstOrDefault(t => t.Mode == CRUD.U)?.Data ?? Array.Empty<Domain.Entities.ClientExtendData>();
            var deleteExtendData = gExtendData.FirstOrDefault(t => t.Mode == CRUD.D)?.Data ?? Array.Empty<Domain.Entities.ClientExtendData>();

            var extendData = entity.ExtendData
                                   .Join(deleteExtendData, t => new { t.Id, t.Key }, t => new { t.Id, t.Key }, (src, desc) => src)
                                   .ToArray();

            if (extendData.Any()) dsExtendData.RemoveRange(extendData);

            if (createExtendData.Any()) dsExtendData.AddRange(createExtendData);

            extendData = entity.ExtendData
                               .Join(updateExtendData, t => new { t.Id, t.Key }, t => new { t.Id, t.Key },
                                     (src, desc) =>
                                     {
                                         src.Value = desc.Value.ToUpper();

                                         return src;
                                     })
                               .ToArray();

            dsExtendData.UpdateRange(extendData);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.SuccessNoContent;
    }
}

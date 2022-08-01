using Microsoft.EntityFrameworkCore;
using Netcorext.Algorithms;
using Netcorext.Auth.Domain.Entities;
using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.EntityFramework.UserIdentityPattern.Extensions;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Hash;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User;

public class UpdateUserHandler : IRequestHandler<UpdateUser, Result>
{
    private readonly DatabaseContext _context;

    public UpdateUserHandler(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateUser request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.User>();
        var dsRole = _context.Set<UserRole>();
        var dsExtendData = _context.Set<UserExtendData>();
        var dsExternalLogin = _context.Set<UserExternalLogin>();

        if (!await ds.AnyAsync(t => t.Id == request.Id, cancellationToken)) return Result.NotFound;
        if (!request.Username.IsEmpty() && await ds.AnyAsync(t => t.Id != request.Id && t.NormalizedUsername == request.Username.ToUpper(), cancellationToken)) return Result.Conflict;

        var entity = ds.Include(t => t.ExtendData)
                       .Include(t => t.Roles)
                       .Include(t => t.ExternalLogins)
                       .First(t => t.Id == request.Id);

        _context.Entry(entity)
                .UpdateProperty(t => t.Username, request.Username)
                .UpdateProperty(t => t.NormalizedUsername, request.Username?.ToUpper())
                .UpdateProperty(t => t.Password, request.Password?.Pbkdf2HashCode(entity.CreationDate.ToUnixTimeMilliseconds()))
                .UpdateProperty(t => t.RequiredChangePassword, false)
                .UpdateProperty(t => t.Email, request.Email, true, user =>
                                                                   {
                                                                       _context.Entry(user).UpdateProperty(t => t.EmailConfirmed, false);
                                                                       _context.Entry(user).UpdateProperty(t => t.NormalizedEmail, request.Email?.ToUpper());
                                                                   })
                .UpdateProperty(t => t.PhoneNumber, request.PhoneNumber, true, user => { _context.Entry(user).UpdateProperty(t => t.PhoneNumberConfirmed, false); })
                .UpdateProperty(t => t.TokenExpireSeconds, request.TokenExpireSeconds)
                .UpdateProperty(t => t.RefreshTokenExpireSeconds, request.RefreshTokenExpireSeconds)
                .UpdateProperty(t => t.CodeExpireSeconds, request.CodeExpireSeconds)
                .UpdateProperty(t => t.RequiredChangePassword, request.RequiredChangePassword)
                .UpdateProperty(t => t.TwoFactorEnabled, request.TwoFactorEnabled, true, user => { _context.Entry(user).UpdateProperty(t => t.Otp, user.TwoFactorEnabled ? Otp.GenerateRandomKey().ToBase32String() : null); })
                .UpdateProperty(t => t.Otp, request.TwoFactorEnabled.HasValue && request.TwoFactorEnabled.Value ? Otp.GenerateRandomKey().ToBase32String() : null)
                .UpdateProperty(t => t.Disabled, request.Disabled, true, user =>
                                                                         {
                                                                             if (user.Disabled)
                                                                             {
                                                                                 _context.Entry(user).UpdateProperty(t => t.AccessFailedCount, 0);
                                                                             }
                                                                         });

        if (request.Roles != null && request.Roles.Any())
        {
            var gRoles = request.Roles
                                .GroupBy(t => t.CRUD, (mode, data) => new
                                                                      {
                                                                          Mode = mode,
                                                                          Data = data.Select(t => new UserRole
                                                                                                  {
                                                                                                      Id = entity.Id,
                                                                                                      RoleId = t.RoleId,
                                                                                                      ExpireDate = t.ExpireDate
                                                                                                  })
                                                                                     .ToArray()
                                                                      })
                                .ToArray();

            var createRoles = gRoles.FirstOrDefault(t => t.Mode == CRUD.C)?.Data ?? Array.Empty<UserRole>();
            var updateRoles = gRoles.FirstOrDefault(t => t.Mode == CRUD.U)?.Data ?? Array.Empty<UserRole>();
            var deleteRoles = gRoles.FirstOrDefault(t => t.Mode == CRUD.D)?.Data ?? Array.Empty<UserRole>();

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
                                     .GroupBy(t => t.CRUD, (mode, data) => new
                                                                           {
                                                                               Mode = mode,
                                                                               Data = data.Select(t => new UserExtendData
                                                                                                       {
                                                                                                           Id = entity.Id,
                                                                                                           Key = t.Key.ToUpper(),
                                                                                                           Value = t.Value
                                                                                                       })
                                                                                          .ToArray()
                                                                           })
                                     .ToArray();

            var createExtendData = gExtendData.FirstOrDefault(t => t.Mode == CRUD.C)?.Data ?? Array.Empty<UserExtendData>();
            var updateExtendData = gExtendData.FirstOrDefault(t => t.Mode == CRUD.U)?.Data ?? Array.Empty<UserExtendData>();
            var deleteExtendData = gExtendData.FirstOrDefault(t => t.Mode == CRUD.D)?.Data ?? Array.Empty<UserExtendData>();

            var extendData = entity.ExtendData
                                   .Join(deleteExtendData, t => new { t.Id, t.Key }, t => new { t.Id, t.Key }, (src, desc) => src)
                                   .ToArray();

            if (extendData.Any()) dsExtendData.RemoveRange(extendData);

            if (createExtendData.Any()) dsExtendData.AddRange(createExtendData);

            extendData = entity.ExtendData
                               .Join(updateExtendData,
                                     t => new { t.Id, t.Key }, t => new { t.Id, t.Key },
                                     (src, desc) =>
                                     {
                                         src.Value = desc.Value;

                                         return src;
                                     })
                               .ToArray();

            dsExtendData.UpdateRange(extendData);
        }

        if (request.ExternalLogins != null && request.ExternalLogins.Any())
        {
            var gExternalLogins = request.ExternalLogins
                                         .GroupBy(t => t.CRUD, (mode, data) => new
                                                                               {
                                                                                   Mode = mode,
                                                                                   Data = data.Select(t => new UserExternalLogin
                                                                                                           {
                                                                                                               Id = entity.Id,
                                                                                                               Provider = t.Provider,
                                                                                                               UniqueId = t.UniqueId
                                                                                                           })
                                                                                              .ToArray()
                                                                               })
                                         .ToArray();

            var createExternalLogins = gExternalLogins.FirstOrDefault(t => t.Mode == CRUD.C)?.Data ?? Array.Empty<UserExternalLogin>();
            var updateExternalLogins = gExternalLogins.FirstOrDefault(t => t.Mode == CRUD.U)?.Data ?? Array.Empty<UserExternalLogin>();
            var deleteExternalLogins = gExternalLogins.FirstOrDefault(t => t.Mode == CRUD.D)?.Data ?? Array.Empty<UserExternalLogin>();

            var externalLogins = entity.ExternalLogins
                                       .Join(deleteExternalLogins, t => new { t.Id, t.Provider }, t => new { t.Id, t.Provider }, (src, desc) => src)
                                       .ToArray();

            if (externalLogins.Any()) dsExternalLogin.RemoveRange(externalLogins);

            if (createExternalLogins.Any()) dsExternalLogin.AddRange(createExternalLogins);

            externalLogins = entity.ExternalLogins
                                   .Join(updateExternalLogins, t => new { t.Id, t.Provider }, t => new { t.Id, t.Provider },
                                         (src, desc) =>
                                         {
                                             src.UniqueId = desc.UniqueId;

                                             return src;
                                         })
                                   .ToArray();

            dsExternalLogin.UpdateRange(externalLogins);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.SuccessNoContent;
    }
}
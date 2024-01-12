using Microsoft.EntityFrameworkCore;
using Netcorext.Algorithms;
using Netcorext.Auth.API.Settings;
using Netcorext.Auth.Domain.Entities;
using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.EntityFramework.UserIdentityPattern.Extensions;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Hash;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Commands;

public class UpdateUserHandler : IRequestHandler<UpdateUser, Result>
{
    private readonly DatabaseContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISnowflake _snowflake;

    public UpdateUserHandler(DatabaseContextAdapter context, IHttpContextAccessor httpContextAccessor, ISnowflake snowflake)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _snowflake = snowflake;
    }

    public async Task<Result> Handle(UpdateUser request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.User>();
        var dsRole = _context.Set<UserRole>();
        var dsExtendData = _context.Set<UserExtendData>();
        var dsExternalLogin = _context.Set<UserExternalLogin>();
        var dsPermission = _context.Set<Domain.Entities.Permission>();
        var dsPermissionCondition = _context.Set<UserPermissionCondition>();
        var dsToken = _context.Set<Token>();

        if (!await ds.AnyAsync(t => t.Id == request.Id, cancellationToken)) return Result.NotFound;
        if (!request.Username.IsEmpty() && await ds.AnyAsync(t => t.Id != request.Id && t.NormalizedUsername == request.Username.ToUpper(), cancellationToken)) return Result.Conflict;

        if (request.PermissionConditions != null && request.PermissionConditions.Any())
        {
            var permissionIds = request.PermissionConditions
                                       .Select(t => t.PermissionId)
                                       .ToArray();

            if (dsPermission.Count(t => permissionIds.Contains(t.Id)) != permissionIds.Length)
                return Result.DependencyNotFound;
        }

        var entity = ds.Include(t => t.ExtendData)
                       .Include(t => t.Roles)
                       .Include(t => t.ExternalLogins)
                       .Include(t => t.PermissionConditions)
                       .First(t => t.Id == request.Id);

        _context.Entry(entity)
                .UpdateProperty(t => t.Username, request.Username)
                .UpdateProperty(t => t.NormalizedUsername, request.Username?.ToUpper())
                .UpdateProperty(t => t.DisplayName, request.DisplayName)
                .UpdateProperty(t => t.NormalizedDisplayName, request.DisplayName?.ToUpper())
                .UpdateProperty(t => t.Password, request.Password?.Pbkdf2HashCode(entity.CreationDate.ToUnixTimeMilliseconds()))
                .UpdateProperty(t => t.RequiredChangePassword, false)
                .UpdateProperty(t => t.Email, request.Email, true, user =>
                                                                   {
                                                                       _context.Entry(user).UpdateProperty(t => t.EmailConfirmed, false);
                                                                       _context.Entry(user).UpdateProperty(t => t.NormalizedEmail, request.Email?.ToUpper());
                                                                   })
                .UpdateProperty(t => t.PhoneNumber, request.PhoneNumber, true, user => { _context.Entry(user).UpdateProperty(t => t.PhoneNumberConfirmed, false); })
                .UpdateProperty(t => t.AllowedRefreshToken, request.AllowedRefreshToken)
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
                                .GroupBy(t => t.Crud, (mode, data) => new
                                                                      {
                                                                          Mode = mode,
                                                                          Data = data.Select(t => new UserRole
                                                                                                  {
                                                                                                      Id = entity.Id,
                                                                                                      RoleId = t.RoleId,
                                                                                                      ExpireDate = t.ExpireDate ?? Core.Constants.MaxDateTime
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

            var stringId = entity.Id.ToString();
            var tokens = dsToken.Where(t => t.Revoked != TokenRevoke.Both && t.ResourceType == ResourceType.User && t.ResourceId == stringId);
            var hsTokens = new HashSet<string>();

            foreach (var token in tokens)
            {
                hsTokens.Add(token.AccessToken);

                if (!token.RefreshToken.IsEmpty())
                    hsTokens.Add(token.RefreshToken);

                _context.Entry(token).UpdateProperty(t => t.Revoked, TokenRevoke.Both);
            }

            _httpContextAccessor.HttpContext?.Items.Add(ConfigSettings.QUEUES_TOKEN_REVOKE_EVENT, hsTokens.ToArray());
        }

        if (request.ExtendData != null && request.ExtendData.Any())
        {
            var gExtendData = request.ExtendData
                                     .GroupBy(t => t.Crud, (mode, data) => new
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
                                         .GroupBy(t => t.Crud, (mode, data) => new
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

        if (request.PermissionConditions != null && request.PermissionConditions.Any())
        {
            var gPermissionCondition = request.PermissionConditions
                                              .GroupBy(t => t.Crud, (mode, permissionCondition) => new
                                                                                                   {
                                                                                                       Mode = mode,
                                                                                                       Data = permissionCondition.Select(t => new UserPermissionCondition
                                                                                                                                              {
                                                                                                                                                  Id = t.Id ?? _snowflake.Generate(),
                                                                                                                                                  UserId = entity.Id,
                                                                                                                                                  PermissionId = t.PermissionId,
                                                                                                                                                  Group = t.Group?.ToUpper(),
                                                                                                                                                  Key = t.Key.ToUpper(),
                                                                                                                                                  Value = t.Value.ToUpper(),
                                                                                                                                                  ExpireDate = t.ExpireDate ?? Core.Constants.MaxDateTime
                                                                                                                                              })
                                                                                                                                 .ToArray()
                                                                                                   })
                                              .ToArray();

            var createPermissionCondition = gPermissionCondition.FirstOrDefault(t => t.Mode == CRUD.C)?.Data ?? Array.Empty<UserPermissionCondition>();
            var updatePermissionCondition = gPermissionCondition.FirstOrDefault(t => t.Mode == CRUD.U)?.Data ?? Array.Empty<UserPermissionCondition>();
            var deletePermissionCondition = gPermissionCondition.FirstOrDefault(t => t.Mode == CRUD.D)?.Data ?? Array.Empty<UserPermissionCondition>();

            var permissionCondition = entity.PermissionConditions
                                            .Join(deletePermissionCondition, t => t.Id, t => t.Id, (src, desc) => src)
                                            .ToArray();

            if (permissionCondition.Any()) dsPermissionCondition.RemoveRange(permissionCondition);

            if (createPermissionCondition.Any()) dsPermissionCondition.AddRange(createPermissionCondition);

            permissionCondition = entity.PermissionConditions
                                        .Join(updatePermissionCondition, t => t.Id, t => t.Id,
                                              (o, i) =>
                                              {
                                                  o.PermissionId = i.PermissionId;
                                                  o.Group = i.Group;
                                                  o.Value = i.Value;
                                                  o.ExpireDate = i.ExpireDate;

                                                  return o;
                                              })
                                        .ToArray();

            dsPermissionCondition.UpdateRange(permissionCondition);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.SuccessNoContent;
    }
}

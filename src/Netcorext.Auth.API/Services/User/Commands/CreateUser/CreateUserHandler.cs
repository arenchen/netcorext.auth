using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netcorext.Algorithms;
using Netcorext.Auth.Domain.Entities;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Hash;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Commands;

public class CreateUserHandler : IRequestHandler<CreateUser, Result<long?>>
{
    private readonly DatabaseContext _context;
    private readonly ISnowflake _snowflake;
    private readonly AuthOptions _config;

    public CreateUserHandler(DatabaseContextAdapter context, ISnowflake snowflake, IOptions<AuthOptions> config)
    {
        _context = context;
        _snowflake = snowflake;
        _config = config.Value;
    }

    public async Task<Result<long?>> Handle(CreateUser request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.User>();
        var dsPermission = _context.Set<Domain.Entities.Permission>();

        if (await ds.AnyAsync(t => t.NormalizedUsername == request.Username.ToUpper(), cancellationToken))
            return Result<long?>.Conflict;

        if (request.CustomId.HasValue && await ds.AnyAsync(t => t.Id == request.CustomId, cancellationToken))
            return Result<long?>.Conflict;

        if (request.PermissionConditions?.Any() == true)
        {
            var permissionIds = request.PermissionConditions.Select(t => t.PermissionId).Distinct().ToArray();

            if (dsPermission.Count(t => permissionIds.Contains(t.Id)) != permissionIds.Length)
                return Result<long?>.DependencyNotFound;
        }

        var id = request.CustomId ?? _snowflake.Generate();
        var creationDate = DateTimeOffset.UtcNow;

        var entity = ds.Add(new Domain.Entities.User
                            {
                                Id = id,
                                Username = request.Username,
                                NormalizedUsername = request.Username.ToUpper(),
                                DisplayName = request.DisplayName ?? request.Username,
                                NormalizedDisplayName = (request.DisplayName ?? request.Username).ToUpper(),
                                Password = request.Password?.Pbkdf2HashCode(creationDate.ToUnixTimeMilliseconds()),
                                Email = request.Email,
                                NormalizedEmail = request.Email?.ToUpper(),
                                PhoneNumber = request.PhoneNumber,
                                Otp = request.TwoFactorEnabled ? Otp.GenerateRandomKey().ToBase32String() : null,
                                TwoFactorEnabled = request.TwoFactorEnabled,
                                RequiredChangePassword = request.RequiredChangePassword,
                                AllowedRefreshToken = request.AllowedRefreshToken,
                                TokenExpireSeconds = request.TokenExpireSeconds ?? _config.TokenExpireSeconds,
                                RefreshTokenExpireSeconds = request.RefreshTokenExpireSeconds ?? _config.RefreshTokenExpireSeconds,
                                CodeExpireSeconds = request.CodeExpireSeconds ?? _config.CodeExpireSeconds,
                                Roles = request.Roles?
                                               .Select(t => new UserRole
                                                            {
                                                                Id = id,
                                                                RoleId = t.RoleId,
                                                                ExpireDate = t.ExpireDate ?? Core.Constants.MaxDateTime
                                                            })
                                               .ToArray() ?? Array.Empty<UserRole>(),
                                ExtendData = request.ExtendData?
                                                    .Select(t => new UserExtendData
                                                                 {
                                                                     Id = id,
                                                                     Key = t.Key.ToUpper(),
                                                                     Value = t.Value.ToUpper()
                                                                 })
                                                    .ToArray() ?? Array.Empty<UserExtendData>(),
                                ExternalLogins = request.ExternalLogins?
                                                        .Select(t => new UserExternalLogin
                                                                     {
                                                                         Id = id,
                                                                         Provider = t.Provider,
                                                                         UniqueId = t.UniqueId
                                                                     })
                                                        .ToArray() ?? Array.Empty<UserExternalLogin>(),
                                PermissionConditions = request.PermissionConditions?
                                                              .Select(t => new UserPermissionCondition
                                                                           {
                                                                               Id = _snowflake.Generate(),
                                                                               UserId = id,
                                                                               PermissionId = t.PermissionId,
                                                                               Group = t.Group?.ToUpper(),
                                                                               Key = t.Key.ToUpper(),
                                                                               Value = t.Value.ToUpper(),
                                                                               ExpireDate = t.ExpireDate ?? Core.Constants.MaxDateTime
                                                                           })
                                                              .ToArray() ?? Array.Empty<UserPermissionCondition>()
                            });

        await _context.SaveChangesAsync(e =>
                                        {
                                            e.CreationDate = creationDate;
                                            e.ModificationDate = creationDate;
                                        }, cancellationToken);

        return Result<long?>.SuccessCreated.Clone(entity.Entity.Id);
    }
}

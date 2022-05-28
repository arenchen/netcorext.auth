using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netcorext.Algorithms;
using Netcorext.Auth.Domain.Entities;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Hash;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User;

public class CreateUserHandler : IRequestHandler<CreateUser, Result<long?>>
{
    private readonly DatabaseContext _context;
    private readonly ISnowflake _snowflake;
    private readonly AuthOptions _config;

    public CreateUserHandler(DatabaseContext context, ISnowflake snowflake, IOptions<AuthOptions> config)
    {
        _context = context;
        _snowflake = snowflake;
        _config = config.Value;
    }

    public async Task<Result<long?>> Handle(CreateUser request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.User>();

        if (await ds.AnyAsync(t => t.NormalizedUsername == request.Username.ToUpper(), cancellationToken)) return Result<long?>.Conflict;

        var id = _snowflake.Generate();
        var creationDate = DateTimeOffset.UtcNow;

        var entity = ds.Add(new Domain.Entities.User
                            {
                                Id = id,
                                Username = request.Username,
                                NormalizedUsername = request.Username.ToUpper(),
                                Password = request.Password!.Pbkdf2HashCode(creationDate.ToUnixTimeMilliseconds()),
                                Email = request.Email,
                                NormalizedEmail = request.Email?.ToUpper(),
                                PhoneNumber = request.PhoneNumber,
                                Otp = request.TwoFactorEnabled ? Otp.GenerateRandomKey().ToBase32String() : null,
                                TwoFactorEnabled = request.TwoFactorEnabled,
                                RequiredChangePassword = request.RequiredChangePassword,
                                TokenExpireSeconds = request.TokenExpireSeconds ?? _config.TokenExpireSeconds,
                                RefreshTokenExpireSeconds = request.RefreshTokenExpireSeconds ?? _config.RefreshTokenExpireSeconds,
                                CodeExpireSeconds = request.CodeExpireSeconds ?? _config.CodeExpireSeconds,
                                Roles = request.Roles?
                                               .Select(t => new UserRole
                                                            {
                                                                Id = id,
                                                                RoleId = t.RoleId,
                                                                ExpireDate = t.ExpireDate
                                                            })
                                               .ToArray() ?? Array.Empty<UserRole>(),
                                ExtendData = request.ExtendData?
                                                    .Select(t => new UserExtendData
                                                                 {
                                                                     Id = id,
                                                                     Key = t.Key,
                                                                     Value = t.Value
                                                                 })
                                                    .ToArray() ?? Array.Empty<UserExtendData>(),
                                ExternalLogins = request.ExternalLogins?
                                                        .Select(t => new UserExternalLogin
                                                                     {
                                                                         Id = id,
                                                                         Provider = t.Provider,
                                                                         UniqueId = t.UniqueId
                                                                     })
                                                        .ToArray() ?? Array.Empty<UserExternalLogin>()
                            });

        await _context.SaveChangesAsync(e =>
                                        {
                                            e.CreationDate = creationDate;
                                            e.ModificationDate = creationDate;
                                        }, cancellationToken);

        return Result<long?>.SuccessCreated.Clone(entity.Entity.Id);
    }
}
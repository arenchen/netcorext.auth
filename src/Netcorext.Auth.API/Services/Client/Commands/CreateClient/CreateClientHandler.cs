using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netcorext.Algorithms;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Hash;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Client.Commands;

public class CreateClientHandler : IRequestHandler<CreateClient, Result<long?>>
{
    private readonly DatabaseContext _context;
    private readonly ISnowflake _snowflake;
    private readonly AuthOptions _authOptions;

    public CreateClientHandler(DatabaseContextAdapter context, ISnowflake snowflake, IOptions<AuthOptions> authOptions)
    {
        _context = context;
        _snowflake = snowflake;
        _authOptions = authOptions.Value;
    }

    public async Task<Result<long?>> Handle(CreateClient request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Client>();

        if (await ds.AnyAsync(t => t.Name.ToUpper() == request.Name.ToUpper(), cancellationToken))
            return Result<long?>.Conflict;

        var id = _snowflake.Generate();
        var creationDate = DateTimeOffset.UtcNow;

        var entity = ds.Add(new Domain.Entities.Client
                            {
                                Id = id,
                                Name = request.Name,
                                Secret = request.Secret.Pbkdf2HashCode(creationDate.ToUnixTimeMilliseconds()),
                                CallbackUrl = request.CallbackUrl,
                                AllowedRefreshToken = request.AllowedRefreshToken,
                                TokenExpireSeconds = request.TokenExpireSeconds ?? _authOptions.TokenExpireSeconds,
                                RefreshTokenExpireSeconds = request.RefreshTokenExpireSeconds ?? _authOptions.RefreshTokenExpireSeconds,
                                CodeExpireSeconds = request.CodeExpireSeconds ?? _authOptions.CodeExpireSeconds,
                                Disabled = request.Disabled,
                                Roles = request.Roles?
                                               .Select(t => new Domain.Entities.ClientRole
                                                            {
                                                                Id = id,
                                                                RoleId = t.RoleId,
                                                                ExpireDate = t.ExpireDate ?? Core.Constants.MaxDateTime
                                                            })
                                               .ToArray() ?? Array.Empty<Domain.Entities.ClientRole>(),
                                ExtendData = request.ExtendData?
                                                    .Select(t => new Domain.Entities.ClientExtendData
                                                                 {
                                                                     Id = id,
                                                                     Key = t.Key.ToUpper(),
                                                                     Value = t.Value.ToUpper()
                                                                 })
                                                    .ToArray() ?? Array.Empty<Domain.Entities.ClientExtendData>()
                            });

        await _context.SaveChangesAsync(e =>
                                        {
                                            e.CreationDate = creationDate;
                                            e.ModificationDate = creationDate;
                                        }, cancellationToken);

        return Result<long?>.SuccessCreated.Clone(entity.Entity.Id);
    }
}

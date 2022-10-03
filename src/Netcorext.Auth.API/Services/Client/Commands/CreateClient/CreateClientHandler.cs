using Microsoft.EntityFrameworkCore;
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

    public CreateClientHandler(DatabaseContext context, ISnowflake snowflake)
    {
        _context = context;
        _snowflake = snowflake;
    }

    public async Task<Result<long?>> Handle(CreateClient request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Client>();

        if (await ds.AnyAsync(t => t.Name.ToUpper() == request.Name.ToUpper(), cancellationToken)) return Result<long?>.Conflict;

        var id = _snowflake.Generate();
        var creationDate = DateTimeOffset.UtcNow;

        var entity = ds.Add(new Domain.Entities.Client
                            {
                                Id = id,
                                Name = request.Name!,
                                Secret = request.Secret!.Pbkdf2HashCode(creationDate.ToUnixTimeMilliseconds()),
                                CallbackUrl = request.CallbackUrl,
                                TokenExpireSeconds = request.TokenExpireSeconds,
                                RefreshTokenExpireSeconds = request.RefreshTokenExpireSeconds,
                                CodeExpireSeconds = request.CodeExpireSeconds,
                                Disabled = request.Disabled,
                                Roles = request.Roles?
                                               .Select(t => new Domain.Entities.ClientRole
                                                            {
                                                                Id = id,
                                                                RoleId = t.RoleId,
                                                                ExpireDate = t.ExpireDate
                                                            })
                                               .ToArray() ?? Array.Empty<Domain.Entities.ClientRole>(),
                                ExtendData = request.ExtendData?
                                                    .Select(t => new Domain.Entities.ClientExtendData
                                                                 {
                                                                     Id = id,
                                                                     Key = t.Key.ToUpper(),
                                                                     Value = t.Value
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
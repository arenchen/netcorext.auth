using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Hash;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Client.Queries;

public class ValidateClientHandler : IRequestHandler<ValidateClient, Result>
{
    private readonly DatabaseContext _context;

    public ValidateClientHandler(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(ValidateClient request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Client>();

        if (!await ds.AnyAsync(t => t.Id == request.Id, cancellationToken)) return Result.UsernameOrPasswordIncorrect;

        var entity = await ds.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (entity == null) return Result.UsernameOrPasswordIncorrect;

        var secret = request.Secret?.Pbkdf2HashCode(entity.CreationDate.ToUnixTimeMilliseconds());

        if (entity.Secret != secret) return Result.UsernameOrPasswordIncorrect;

        if (entity.Disabled) return Result.AccountIsDisabled;

        return Result.Success;
    }
}
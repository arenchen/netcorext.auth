using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Hash;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Client;

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

        var entity = await ds.FirstAsync(t => t.Id == request.Id, cancellationToken);

        var secret = request.Secret?.Pbkdf2HashCode(entity.CreationDate.ToUnixTimeMilliseconds());

        return entity.Secret == secret ? Result.Success : Result.UsernameOrPasswordIncorrect;
    }
}
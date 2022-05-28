using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Hash;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authorization.Services.User;

public class ValidateUserHandler : IRequestHandler<ValidateUser, Result>
{
    private readonly DatabaseContext _context;

    public ValidateUserHandler(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(ValidateUser request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.User>();

        Expression<Func<Domain.Entities.User, bool>> predicate = p => true;

        if (!request.Id.IsEmpty()) predicate = predicate.And(t => t.Id == request.Id);
        if (!request.Username.IsEmpty()) predicate = predicate.And(t => t.NormalizedUsername == request.Username.ToUpper());

        if (!await ds.AnyAsync(predicate, cancellationToken)) return Result.UsernameOrPasswordIncorrect;

        var entity = await ds.FirstAsync(predicate, cancellationToken);

        var secret = request.Password?.Pbkdf2HashCode(entity.CreationDate.ToUnixTimeMilliseconds());

        return entity.Password == secret ? Result.Success : Result.UsernameOrPasswordIncorrect;
    }
}
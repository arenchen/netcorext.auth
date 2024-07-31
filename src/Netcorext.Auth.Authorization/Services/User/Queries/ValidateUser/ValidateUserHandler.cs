using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Hash;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authorization.Services.User.Queries;

public class ValidateUserHandler : IRequestHandler<ValidateUser, Result>
{
    private readonly DatabaseContext _context;

    public ValidateUserHandler(DatabaseContextAdapter context)
    {
        _context = context.Slave;
    }

    public async Task<Result> Handle(ValidateUser request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.User>();

        Expression<Func<Domain.Entities.User, bool>> predicate = p => true;

        if (!request.Id.IsEmpty()) predicate = predicate.And(t => t.Id == request.Id);
        if (!request.Username.IsEmpty()) predicate = predicate.And(t => t.NormalizedUsername == request.Username.ToUpper());

        var entity = await ds.FirstOrDefaultAsync(predicate, cancellationToken);

        if (entity == null)
            return Result.UsernameOrPasswordIncorrect;

        var secret = request.Password?.Pbkdf2HashCode(entity.CreationDate.ToUnixTimeMilliseconds());

        return entity.Password == secret ? Result.Success : Result.UsernameOrPasswordIncorrect;
    }
}

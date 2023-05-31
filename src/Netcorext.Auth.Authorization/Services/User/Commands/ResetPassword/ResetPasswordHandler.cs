using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.EntityFramework.UserIdentityPattern.Extensions;
using Netcorext.Extensions.Hash;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authorization.Services.User.Commands;

public class ResetPasswordHandler : IRequestHandler<ResetPassword, Result>
{
    private readonly DatabaseContext _context;

    public ResetPasswordHandler(DatabaseContextAdapter context)
    {
        _context = context;
    }

    public async Task<Result> Handle(ResetPassword request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.User>();

        Expression<Func<Domain.Entities.User, bool>> predicate = u => u.Id == request.Id;

        var entity = await ds.FirstOrDefaultAsync(predicate, cancellationToken);

        if (entity == null) return Result.NotFound;

        _context.Entry(entity).UpdateProperty(t => t.Password, request.Password!.Pbkdf2HashCode(entity.CreationDate.ToUnixTimeMilliseconds()));
        _context.Entry(entity).UpdateProperty(t => t.RequiredChangePassword, false);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
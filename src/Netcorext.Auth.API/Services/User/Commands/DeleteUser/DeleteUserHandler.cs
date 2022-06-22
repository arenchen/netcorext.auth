using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User;

public class DeleteUserHandler : IRequestHandler<DeleteUser, Result>
{
    private readonly DatabaseContext _context;

    public DeleteUserHandler(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteUser request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.User>();

        var qUser = ds.Where(t => t.Id == request.Id);

        if (!qUser.Any()) return Result.SuccessNoContent;

        ds.RemoveRange(qUser);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.SuccessNoContent;
    }
}
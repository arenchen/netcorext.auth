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
        var dsRole = _context.Set<Domain.Entities.UserRole>();
        var dsExtendData = _context.Set<Domain.Entities.UserExtendData>();
        var dsExternalLogin = _context.Set<Domain.Entities.UserExternalLogin>();

        var qUser = ds.Where(t => t.Id == request.Id);

        if (!qUser.Any()) return Result.Success;

        var qRole = dsRole.Where(t => t.Id == request.Id);

        if (qRole.Any()) dsRole.RemoveRange(qRole);

        var qExtendData = dsExtendData.Where(t => t.Id == request.Id);

        if (qExtendData.Any()) dsExtendData.RemoveRange(qExtendData);

        var qExternalLogin = dsExternalLogin.Where(t => t.Id == request.Id);

        if (qExternalLogin.Any()) dsExternalLogin.RemoveRange(qExternalLogin);

        ds.RemoveRange(qUser);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
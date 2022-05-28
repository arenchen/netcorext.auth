using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User;

public class ExistsUserHandler : IRequestHandler<ExistsUser, Result>
{
    private readonly DatabaseContext _context;

    public ExistsUserHandler(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(ExistsUser request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.User>();

        if (await ds.AnyAsync(t => t.NormalizedUsername == request.Username!.ToUpper(), cancellationToken)) return Result.Success;

        return Result.NotFound;
    }
}
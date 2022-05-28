using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role;

public class ExistsRoleHandler : IRequestHandler<ExistsRole, Result>
{
    private readonly DatabaseContext _context;

    public ExistsRoleHandler(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(ExistsRole request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Role>();

        if (await ds.AnyAsync(t => t.Name.ToUpper() == request.Name!.ToUpper(), cancellationToken))
        {
            return Result.Success;
        }

        return Result.NotFound;
    }
}
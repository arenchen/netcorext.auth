using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User;

public class GetUserRoleHandler : IRequestHandler<GetUserRole, Result<IEnumerable<Models.SimpleUserRole>>>
{
    private readonly DatabaseContext _context;

    public GetUserRoleHandler(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Result<IEnumerable<Models.SimpleUserRole>>> Handle(GetUserRole request, CancellationToken cancellationToken = new CancellationToken())
    {
        var ds = _context.Set<Domain.Entities.UserRole>();

        var queryEntities = ds.Include(t => t.Role)
                              .Where(t => t.Id == request.Id)
                              .AsNoTracking();

        var content = queryEntities.Select(t => new Models.SimpleUserRole
                                                {
                                                    Id = t.Id,
                                                    RoleId = t.RoleId,
                                                    Name = t.Role.Name
                                                });

        if (!await content.AnyAsync(cancellationToken)) content = null;

        return Result<IEnumerable<Models.SimpleUserRole>>.Success.Clone(content?.ToArray());
    }
}
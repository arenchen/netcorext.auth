using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserRoleHandler : IRequestHandler<GetUserRole, Result<IEnumerable<Models.SimpleUserRole>>>
{
    private readonly DatabaseContext _context;

    public GetUserRoleHandler(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Result<IEnumerable<Models.SimpleUserRole>>> Handle(GetUserRole request, CancellationToken cancellationToken = new())
    {
        var ds = _context.Set<Domain.Entities.UserRole>();

        var queryEntities = ds.Where(t => request.Ids.Contains(t.Id) && !t.Role.Disabled)
                              .Include(t => t.Role)
                              .AsNoTracking();

        var content = queryEntities.Select(t => new Models.SimpleUserRole
                                                {
                                                    Id = t.Id,
                                                    RoleId = t.RoleId,
                                                    Name = t.Role.Name,
                                                    CreationDate = t.Role.CreationDate,
                                                    CreatorId = t.Role.CreatorId,
                                                    ModificationDate = t.Role.ModificationDate,
                                                    ModifierId = t.Role.ModifierId
                                                });

        if (!await content.AnyAsync(cancellationToken)) content = null;

        return Result<IEnumerable<Models.SimpleUserRole>>.Success.Clone(content?.ToArray());
    }
}
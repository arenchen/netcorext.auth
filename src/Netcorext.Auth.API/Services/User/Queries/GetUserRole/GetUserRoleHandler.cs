using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Linq;
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

        Expression<Func<Domain.Entities.UserRole, bool>> predicate = p => false;

        predicate = request.Ids.Aggregate(predicate, (current, id) => current.Or(t => t.Id == id));
        
        var queryEntities = ds.Include(t => t.Role)
                              .Where(predicate)
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
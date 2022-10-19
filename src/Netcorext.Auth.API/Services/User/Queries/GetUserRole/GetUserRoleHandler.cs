using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Settings;
using Netcorext.Configuration.Extensions;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserRoleHandler : IRequestHandler<GetUserRole, Result<IEnumerable<Models.SimpleUserRole>>>
{
    private readonly DatabaseContext _context;
    private readonly int _dataSizeLimit;

    public GetUserRoleHandler(DatabaseContext context, IOptions<ConfigSettings> config)
    {
        _context = context;
        _dataSizeLimit = config.Value.Connections.RelationalDb.GetDefault().DataSizeLimit;
    }

    public async Task<Result<IEnumerable<Models.SimpleUserRole>>> Handle(GetUserRole request, CancellationToken cancellationToken = new())
    {
        var ds = _context.Set<Domain.Entities.UserRole>();

        var queryEntities = ds.Where(t => request.Ids.Contains(t.Id) && !t.Role.Disabled)
                              .OrderBy(t => t.Id)
                              .Take(_dataSizeLimit)
                              .AsNoTracking();

        var content = queryEntities.Select(t => new Models.SimpleUserRole
                                                {
                                                    Id = t.Id,
                                                    RoleId = t.RoleId,
                                                    Name = t.Role.Name,
                                                    ExpireDate = t.ExpireDate,
                                                    CreationDate = t.Role.CreationDate,
                                                    CreatorId = t.Role.CreatorId,
                                                    ModificationDate = t.Role.ModificationDate,
                                                    ModifierId = t.Role.ModifierId
                                                });

        if (!await content.AnyAsync(cancellationToken)) content = null;

        return Result<IEnumerable<Models.SimpleUserRole>>.Success.Clone(content?.ToArray());
    }
}
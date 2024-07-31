using System.Linq.Expressions;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Client.Queries;

public class GetClientHandler : IRequestHandler<GetClient, Result<Models.Client[]>>
{
    private readonly DatabaseContext _context;

    public GetClientHandler(DatabaseContextAdapter context)
    {
        _context = context.Slave;
    }

    public Task<Result<Models.Client[]>> Handle(GetClient request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Client>();

        Expression<Func<Domain.Entities.Client, bool>> predicate = p => !p.Disabled;

        if (!request.Ids.IsEmpty())
            predicate = predicate.And(t => request.Ids.Contains(t.Id));

        var entities = ds.Where(predicate)
                         .Select(t => new
                                      {
                                          t.Id,
                                          t.Secret,
                                          Roles = t.Roles
                                                   .Where(t2 => !t2.Role.Disabled)
                                                   .Select(t2 => new
                                                                 {
                                                                     t2.RoleId,
                                                                     t2.ExpireDate
                                                                 }),
                                          t.CreationDate
                                      })
                         .ToArray()
                         .Select(t => new Models.Client
                                      {
                                            Id = t.Id,
                                            Secret = t.Secret,
                                            Roles = t.Roles.ToDictionary(t2 => t2.RoleId, t2 => t2.ExpireDate),
                                            CreationDate = t.CreationDate
                                      })
                         .ToArray();

        return Task.FromResult(Result<Models.Client[]>.Success.Clone(entities));
    }
}

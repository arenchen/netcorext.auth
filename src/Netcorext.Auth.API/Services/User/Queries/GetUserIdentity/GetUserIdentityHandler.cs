using System.Linq.Expressions;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserIdentityHandler : IRequestHandler<GetUserIdentity, Result<IEnumerable<Models.UserIdentity>>>
{
    private readonly DatabaseContext _context;

    public GetUserIdentityHandler(DatabaseContextAdapter context)
    {
        _context = context.Slave;
    }

    public Task<Result<IEnumerable<Models.UserIdentity>>> Handle(GetUserIdentity request, CancellationToken cancellationToken = default)
    {
        Expression<Func<Domain.Entities.User, bool>> predicate = p => false;

        if (request.Ids?.Any() == true)
            predicate = predicate.Or(t => request.Ids.Contains(t.Id));

        if (request.Usernames?.Any() == true)
            predicate = predicate.Or(t => request.Usernames
                                                 .Select(u => u.ToUpper())
                                                 .Contains(t.NormalizedUsername));

        var ds = _context.Set<Domain.Entities.User>();

        var users = ds.Where(predicate)
                      .Select(t => new Models.UserIdentity
                                   {
                                       Id = t.Id,
                                       Username = t.Username,
                                       DisplayName = t.DisplayName
                                   });

        return Task.FromResult(Result<IEnumerable<Models.UserIdentity>>.Success.Clone(users));
    }
}
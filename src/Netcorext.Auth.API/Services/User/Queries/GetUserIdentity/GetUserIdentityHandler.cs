using System.Linq.Expressions;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserIdentityHandler : IRequestHandler<GetUserIdentity, Result<IEnumerable<Models.UserIdentity>>>
{
    private readonly DatabaseContext _context;

    public GetUserIdentityHandler(DatabaseContext context)
    {
        _context = context;
    }

    public Task<Result<IEnumerable<Models.UserIdentity>>> Handle(GetUserIdentity request, CancellationToken cancellationToken = new())
    {
        Expression<Func<Domain.Entities.User, bool>> predicate = p => true;

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
                                       Username = t.Username
                                   });

        return Task.FromResult(Result<IEnumerable<Models.UserIdentity>>.Success.Clone(users));
    }
}
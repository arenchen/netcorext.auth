using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Permission.Queries;

public class GetRouteFunctionHandler : IRequestHandler<GetRouteFunction, Result<IEnumerable<string>>>
{
    private readonly DatabaseContext _context;

    public GetRouteFunctionHandler(DatabaseContextAdapter context)
    {
        _context = context.Slave;
    }

    public Task<Result<IEnumerable<string>>> Handle(GetRouteFunction request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Route>();

        var ids = ds.Select(t => t.FunctionId)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToArray();

        return Task.FromResult(Result<IEnumerable<string>>.Success.Clone(ids, new Paging
                                                                              {
                                                                                  Offset = 0,
                                                                                  Limit = ids.Length,
                                                                                  Count = ids.Length
                                                                              }));
    }
}
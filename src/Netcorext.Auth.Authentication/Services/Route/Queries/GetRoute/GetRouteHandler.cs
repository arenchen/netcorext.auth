using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Route;

public class GetRouteHandler : IRequestHandler<GetRoute, Result<IEnumerable<Models.Route>>>
{
    private readonly DatabaseContext _context;

    public GetRouteHandler(DatabaseContext context)
    {
        _context = context;
    }

    public Task<Result<IEnumerable<Models.Route>>> Handle(GetRoute request, CancellationToken cancellationToken = new CancellationToken())
    {
        var ds = _context.Set<Domain.Entities.Route>();

        Expression<Func<Domain.Entities.Route, bool>> predicate = p => true;
        
        if (request.Ids != null && request.Ids.Any())
        {
            predicate = request.Ids.Aggregate(predicate, (current, id) => current.Or(p => p.Id == id));
        }
        
        var queryEntities = ds.Include(t => t.RouteValues)
                              .Where(predicate)
                              .AsNoTracking();

        var content = queryEntities.Select(t => new Models.Route
                                                {
                                                    Id = t.Id,
                                                    Group = t.Group,
                                                    Protocol = t.Protocol,
                                                    HttpMethod = t.HttpMethod,
                                                    BaseUrl = t.BaseUrl,
                                                    RelativePath = t.RelativePath,
                                                    Template = t.Template,
                                                    FunctionId = t.FunctionId,
                                                    NativePermission = t.NativePermission,
                                                    AllowAnonymous = t.AllowAnonymous,
                                                    Tag = t.Tag,
                                                    CreationDate = t.CreationDate,
                                                    CreatorId = t.CreatorId,
                                                    ModificationDate = t.ModificationDate,
                                                    ModifierId = t.ModifierId,
                                                    RouteValues = t.RouteValues.Select(t2 => new Models.RouteValue
                                                                                             {
                                                                                                 Key = t2.Key,
                                                                                                 Value = t2.Value,
                                                                                                 CreationDate = t2.CreationDate,
                                                                                                 CreatorId = t2.CreatorId,
                                                                                                 ModificationDate = t2.ModificationDate,
                                                                                                 ModifierId = t2.ModifierId
                                                                                             })
                                                });

        return Task.FromResult(!content.Any()
                   ? Result<IEnumerable<Models.Route>>.Success
                   : Result<IEnumerable<Models.Route>>.Success.Clone(content.ToArray()));
    }
}
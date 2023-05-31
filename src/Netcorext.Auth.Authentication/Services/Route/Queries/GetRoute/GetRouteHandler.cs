using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;
using Yarp.ReverseProxy.Forwarder;

namespace Netcorext.Auth.Authentication.Services.Route.Queries;

public class GetRouteHandler : IRequestHandler<GetRoute, Result<IEnumerable<Models.RouteGroup>>>
{
    private readonly DatabaseContext _context;

    public GetRouteHandler(DatabaseContextAdapter context)
    {
        _context = context.Slave;
    }

    public Task<Result<IEnumerable<Models.RouteGroup>>> Handle(GetRoute request, CancellationToken cancellationToken = new())
    {
        var ds = _context.Set<Domain.Entities.RouteGroup>();

        Expression<Func<Domain.Entities.RouteGroup, bool>> predicate = p => request.GroupIds == null;

        if (request.GroupIds != null && request.GroupIds.Any())
        {
            predicate = request.GroupIds.Aggregate(predicate, (current, id) => current.Or(p => p.Id == id));
        }

        var queryEntities = ds.Where(predicate)
                              .AsNoTracking();

        var content = queryEntities.Select(t => new Models.RouteGroup
                                                {
                                                        Id = t.Id,
                                                        Name = t.Name,
                                                        BaseUrl = t.BaseUrl,
                                                        ForwarderRequestConfig = t.ForwarderRequestVersion == null
                                                                                         ? null
                                                                                         : new ForwarderRequestConfig
                                                                                           {
                                                                                                   Version = new Version(t.ForwarderRequestVersion),
                                                                                                   VersionPolicy = t.ForwarderHttpVersionPolicy,
                                                                                                   ActivityTimeout = t.ForwarderActivityTimeout,
                                                                                                   AllowResponseBuffering = t.ForwarderAllowResponseBuffering
                                                                                           },
                                                        Routes = t.Routes.Select(t2 => new Models.Route
                                                                                       {
                                                                                               Protocol = t2.Protocol,
                                                                                               HttpMethod = t2.HttpMethod,
                                                                                               RelativePath = t2.RelativePath,
                                                                                               Template = t2.Template,
                                                                                               FunctionId = t2.FunctionId,
                                                                                               NativePermission = t2.NativePermission,
                                                                                               AllowAnonymous = t2.AllowAnonymous,
                                                                                               Tag = t2.Tag,
                                                                                               RouteValues = t2.RouteValues.Select(t3 => new Models.RouteValue
                                                                                                                                         {
                                                                                                                                                 Key = t3.Key,
                                                                                                                                                 Value = t3.Value
                                                                                                                                         })
                                                                                       })
                                                });

        return Task.FromResult(!content.Any()
                                       ? Result<IEnumerable<Models.RouteGroup>>.Success
                                       : Result<IEnumerable<Models.RouteGroup>>.Success.Clone(content.ToArray()));
    }
}
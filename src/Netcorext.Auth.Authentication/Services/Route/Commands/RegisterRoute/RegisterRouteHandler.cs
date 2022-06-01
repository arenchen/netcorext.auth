using System.Linq.Expressions;
using System.Text.Json;
using FreeRedis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netcorext.Algorithms;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Route;

public class RegisterRouteHandler : IRequestHandler<RegisterRoute, Result>
{
    private readonly DatabaseContext _context;
    private readonly RedisClient _redis;
    private readonly ISnowflake _snowflake;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ConfigSettings _config;

    public RegisterRouteHandler(DatabaseContext context, RedisClient redis, ISnowflake snowflake, IOptions<ConfigSettings> config, IOptions<JsonOptions> jsonOptions)
    {
        _context = context;
        _redis = redis;
        _snowflake = snowflake;
        _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
        _config = config.Value;
    }

    public async Task<Result> Handle(RegisterRoute request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Route>();

        Expression<Func<Domain.Entities.Route, bool>> predicate = p => false;

        predicate = request.Routes!
                           .Select(t => t)
                           .Distinct()
                           .Aggregate(predicate, (current, route) => current.Or(p => p.Group == route.Group && p.RelativePath.ToUpper() == route.RelativePath.ToUpper()));

        var qRoute = ds.Include(t => t.RouteValues)
                       .Where(predicate);

        if (await qRoute.AnyAsync(cancellationToken))
        {
            ds.RemoveRange(qRoute);
        }

        var entRoutes = request.Routes!.Select(t =>
                                               {
                                                   var id = _snowflake.Generate();

                                                   return new Domain.Entities.Route
                                                          {
                                                              Id = id,
                                                              Group = t.Group,
                                                              Protocol = t.Protocol.ToUpper(),
                                                              HttpMethod = t.HttpMethod.ToUpper(),
                                                              BaseUrl = t.BaseUrl,
                                                              RelativePath = t.RelativePath,
                                                              Template = t.Template,
                                                              FunctionId = t.FunctionId,
                                                              NativePermission = t.NativePermission,
                                                              AllowAnonymous = t.AllowAnonymous,
                                                              Tag = t.Tag,
                                                              RouteValues = (t.RouteValues ?? Array.Empty<RegisterRoute.RouteValue>())
                                                                           .Select(t2 => new Domain.Entities.RouteValue
                                                                                         {
                                                                                             Id = id,
                                                                                             Key = t2.Key,
                                                                                             Value = t2.Value
                                                                                         })
                                                                           .ToArray()
                                                          };
                                               })
                               .ToArray();

        await ds.AddRangeAsync(entRoutes, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var ids = entRoutes.Select(t => t.Id).ToArray();
        
        _redis.Publish(_config.Queues[ConfigSettings.QUEUES_ROUTE_CHANGE_EVENT], JsonSerializer.Serialize(ids, _jsonOptions));

        return Result.Success;
    }
}
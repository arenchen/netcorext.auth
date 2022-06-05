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
        var ds = _context.Set<Domain.Entities.RouteGroup>();
        var dsRoute = _context.Set<Domain.Entities.Route>();

        Expression<Func<Domain.Entities.RouteGroup, bool>> predicate = p => false;

        predicate = request.Groups.Aggregate(predicate, (current, g) => current.Or(t => t.Name.ToUpper() == g.Name.ToUpper()));

        var groups = ds.Include(t => t.Routes).ThenInclude(t => t.RouteValues)
                       .Where(predicate)
                       .ToArray();

        var requestGroups = request.Groups.Select(t =>
                                                  {
                                                      var gid = _snowflake.Generate();

                                                      return new Domain.Entities.RouteGroup
                                                             {
                                                                 Id = gid,
                                                                 Name = t.Name,
                                                                 BaseUrl = t.BaseUrl,
                                                                 ForwarderRequestVersion = t.ForwarderRequestVersion,
                                                                 ForwarderHttpVersionPolicy = t.ForwarderHttpVersionPolicy,
                                                                 ForwarderActivityTimeout = t.ForwarderActivityTimeout,
                                                                 ForwarderAllowResponseBuffering = t.ForwarderAllowResponseBuffering,
                                                                 Routes = t.Routes.Select(t2 =>
                                                                                          {
                                                                                              var id = _snowflake.Generate();

                                                                                              return new Domain.Entities.Route
                                                                                                     {
                                                                                                         Id = id,
                                                                                                         GroupId = gid,
                                                                                                         Protocol = t2.Protocol.ToUpper(),
                                                                                                         HttpMethod = t2.HttpMethod.ToUpper(),
                                                                                                         RelativePath = t2.RelativePath,
                                                                                                         Template = t2.Template,
                                                                                                         FunctionId = t2.FunctionId,
                                                                                                         NativePermission = t2.NativePermission,
                                                                                                         AllowAnonymous = t2.AllowAnonymous,
                                                                                                         Tag = t2.Tag,
                                                                                                         RouteValues = (t2.RouteValues ?? Array.Empty<RegisterRoute.RouteValue>())
                                                                                                                      .Select(t3 => new Domain.Entities.RouteValue
                                                                                                                                    {
                                                                                                                                        Id = id,
                                                                                                                                        Key = t3.Key,
                                                                                                                                        Value = t3.Value
                                                                                                                                    })
                                                                                                                      .ToHashSet()
                                                                                                     };
                                                                                          })
                                                                           .ToHashSet()
                                                             };
                                                  })
                                   .ToHashSet();

        var diffGroups = groups.IntersectExcept(requestGroups, t => t.Name);

        if (diffGroups.FirstExcept.Any())
            ds.RemoveRange(diffGroups.FirstExcept);

        if (diffGroups.SecondExcept.Any())
            await ds.AddRangeAsync(diffGroups.SecondExcept, cancellationToken);

        diffGroups.FirstIntersect.Merge(diffGroups.SecondIntersect, t => t.Name,
                                        (src, desc) =>
                                        {
                                            src.BaseUrl = desc.BaseUrl;
                                            src.ForwarderRequestVersion = desc.ForwarderRequestVersion;
                                            src.ForwarderHttpVersionPolicy = desc.ForwarderHttpVersionPolicy;
                                            src.ForwarderActivityTimeout = desc.ForwarderActivityTimeout;
                                            src.ForwarderAllowResponseBuffering = desc.ForwarderAllowResponseBuffering;

                                            dsRoute.RemoveRange(src.Routes);

                                            src.Routes = desc.Routes
                                                             .Select(t =>
                                                                     {
                                                                         t.GroupId = src.Id;

                                                                         return t;
                                                                     })
                                                             .ToHashSet();

                                            return src;
                                        });

        await _context.SaveChangesAsync(cancellationToken);

        var ids = diffGroups.FirstExcept.Select(t => t.Id)
                            .Union(diffGroups.SecondExcept.Select(t => t.Id))
                            .Union(diffGroups.FirstIntersect.Select(t => t.Id))
                            .Distinct()
                            .ToArray();

        _redis.Publish(_config.Queues[ConfigSettings.QUEUES_ROUTE_CHANGE_EVENT], JsonSerializer.Serialize(ids, _jsonOptions));

        return Result.Success;
    }
}
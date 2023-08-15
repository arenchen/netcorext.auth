using FreeRedis;
using Microsoft.Extensions.Options;
using Netcorext.Algorithms;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Auth.Domain.Entities;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;
using Netcorext.Serialization;

namespace Netcorext.Auth.Authentication.Services.Route.Commands;

public class RegisterRouteHandler : IRequestHandler<RegisterRoute, Result>
{
    private readonly DatabaseContext _context;
    private readonly RedisClient _redis;
    private readonly ISerializer _serializer;
    private readonly ISnowflake _snowflake;
    private readonly ILogger<RegisterRouteHandler> _logger;
    private readonly ConfigSettings _config;

    public RegisterRouteHandler(DatabaseContextAdapter context, RedisClient redis, ISerializer serializer, ISnowflake snowflake, IOptions<ConfigSettings> config, ILogger<RegisterRouteHandler> logger)
    {
        _context = context;
        _redis = redis;
        _serializer = serializer;
        _snowflake = snowflake;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<Result> Handle(RegisterRoute request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<RouteGroup>();
        var dsRoute = _context.Set<Domain.Entities.Route>();
        var lsChangeIds = new List<long>();

        foreach (var group in request.Groups)
        {
            try
            {
                if (!await _redis.HSetNxAsync(_config.AppSettings.LockPrefixKey, group.Name, Array.Empty<byte>()))
                    continue;

                var entGroup = ds.FirstOrDefault(t => t.Name.ToUpper() == group.Name.ToUpper());

                if (entGroup == null)
                {
                    entGroup = new RouteGroup
                               {
                                   Id = _snowflake.Generate(),
                                   Name = group.Name
                               };

                    await ds.AddAsync(entGroup, cancellationToken);
                }

                var entRoutes = dsRoute.Where(t => t.GroupId == entGroup.Id);

                dsRoute.RemoveRange(entRoutes);

                entGroup.BaseUrl = group.BaseUrl;
                entGroup.ForwarderRequestVersion = group.ForwarderRequestVersion;
                entGroup.ForwarderHttpVersionPolicy = group.ForwarderHttpVersionPolicy;
                entGroup.ForwarderActivityTimeout = group.ForwarderActivityTimeout;
                entGroup.ForwarderAllowResponseBuffering = group.ForwarderAllowResponseBuffering;

                var routes = group.Routes.Select(t2 =>
                                                 {
                                                     var id = _snowflake.Generate();

                                                     return new Domain.Entities.Route
                                                            {
                                                                Id = id,
                                                                GroupId = entGroup.Id,
                                                                Protocol = t2.Protocol.ToUpper(),
                                                                HttpMethod = t2.HttpMethod.ToUpper(),
                                                                RelativePath = t2.RelativePath,
                                                                Template = t2.Template,
                                                                FunctionId = t2.FunctionId,
                                                                NativePermission = t2.NativePermission,
                                                                AllowAnonymous = t2.AllowAnonymous,
                                                                Tag = t2.Tag,
                                                                RouteValues = (t2.RouteValues ?? Array.Empty<RegisterRoute.RouteValue>())
                                                                             .Select(t3 => new RouteValue
                                                                                           {
                                                                                               Id = id,
                                                                                               Key = t3.Key,
                                                                                               Value = t3.Value
                                                                                           })
                                                                             .ToHashSet()
                                                            };
                                                 });

                await dsRoute.AddRangeAsync(routes, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                lsChangeIds.Add(entGroup.Id);

                await _redis.HDelAsync(_config.AppSettings.LockPrefixKey, group.Name);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{Message}", e.Message);

                await _redis.HDelAsync(_config.AppSettings.LockPrefixKey, group.Name);
            }
        }

        if (lsChangeIds.Any())
            await _redis.PublishAsync(_config.Queues[ConfigSettings.QUEUES_ROUTE_CHANGE_EVENT], await _serializer.SerializeAsync(lsChangeIds, cancellationToken));

        return Result.Success;
    }
}
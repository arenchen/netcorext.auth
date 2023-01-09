using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Services.Route.Queries;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Contracts;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;
using Netcorext.Serialization;
using Netcorext.Worker;
using Yarp.ReverseProxy.Configuration;

namespace Netcorext.Auth.Authentication.Workers;

internal class RouteRunner : IWorkerRunner<AuthWorker>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RedisClient _redis;
    private IDisposable? _subscriber;
    private readonly IMemoryCache _cache;
    private readonly ISerializer _serializer;
    private readonly InMemoryConfigProvider _memoryConfigProvider;
    private readonly ConfigSettings _config;
    private readonly ILogger<RouteRunner> _logger;
    private static readonly SemaphoreSlim RouteUpdateLocker = new(1, 1);

    public RouteRunner(IServiceProvider serviceProvider, RedisClient redis, IMemoryCache cache, ISerializer serializer, IProxyConfigProvider proxyConfigProvider, IOptions<ConfigSettings> config, ILogger<RouteRunner> logger)
    {
        _serviceProvider = serviceProvider;
        _redis = redis;
        _cache = cache;
        _serializer = serializer;
        _memoryConfigProvider = (InMemoryConfigProvider)proxyConfigProvider;
        _config = config.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(AuthWorker worker, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{Message}", nameof(RouteRunner));

        _subscriber?.Dispose();

        _subscriber = _redis.Subscribe(_config.Queues[ConfigSettings.QUEUES_ROUTE_CHANGE_EVENT], async (s, o) => await UpdateRouteAsync(o.ToString(), cancellationToken));

        await UpdateRouteAsync(null, cancellationToken);
    }

    private async Task UpdateRouteAsync(string? ids, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(nameof(UpdateRouteAsync));

            using var scope = _serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            var reqIds = ids == null ? null : _serializer.Deserialize<long[]>(ids);

            var request = new GetRoute
                          {
                              GroupIds = reqIds
                          };

            var result = await dispatcher.SendAsync(request, cancellationToken);

            await RouteUpdateLocker.WaitAsync(cancellationToken);

            if (result.Content == null || result.Code != Result.Success)
                return;

            var cacheRouteGroups = _cache.Get<Dictionary<long, Services.Route.Queries.Models.RouteGroup>>(ConfigSettings.CACHE_ROUTE) ?? new Dictionary<long, Services.Route.Queries.Models.RouteGroup>();

            if (reqIds != null && reqIds.Any())
            {
                var repIds = result.Content.Select(t => t.Id);

                var diffIds = reqIds.Except(repIds);

                diffIds.ForEach(t => cacheRouteGroups.Remove(t));
            }

            foreach (var group in result.Content)
            {
                if (cacheRouteGroups.TryAdd(group.Id, group))
                    continue;

                cacheRouteGroups[group.Id] = group;
            }

            if (request.GroupIds != null)
            {
                var diffIds = request.GroupIds.ExceptBoth(result.Content.Select(t => t.Id));

                foreach (var id in diffIds.First)
                {
                    cacheRouteGroups.Remove(id);
                }
            }

            _cache.Set(ConfigSettings.CACHE_ROUTE, cacheRouteGroups);

            var clusters = cacheRouteGroups.Values
                                           .Select(t => new ClusterConfig
                                                        {
                                                            ClusterId = $"{t.Id}-{t.Name}",
                                                            HttpRequest = t.ForwarderRequestConfig,
                                                            Destinations = new Dictionary<string, DestinationConfig>
                                                                           {
                                                                               {
                                                                                   $"{t.Name}-{t.BaseUrl}",
                                                                                   new DestinationConfig
                                                                                   {
                                                                                       Address = t.BaseUrl
                                                                                   }
                                                                               }
                                                                           }
                                                        })
                                           .ToArray();

            var routes = cacheRouteGroups.Values
                                         .SelectMany(t => t.Routes
                                                           .Select(t2 => new { t2.Protocol, t2.RelativePath })
                                                           .Distinct()
                                                           .Select(t2 => new RouteConfig
                                                                         {
                                                                             ClusterId = $"{t.Id}-{t.Name}",
                                                                             RouteId = $"{t2.Protocol} - {t.BaseUrl}/{t2.RelativePath}",
                                                                             Match = new RouteMatch
                                                                                     {
                                                                                         Path = t2.RelativePath
                                                                                     }
                                                                         }))
                                         .ToArray();

            _memoryConfigProvider.Update(routes, clusters);
        }
        finally
        {
            RouteUpdateLocker.Release();
        }
    }

    public void Dispose()
    {
        _subscriber?.Dispose();
    }
}
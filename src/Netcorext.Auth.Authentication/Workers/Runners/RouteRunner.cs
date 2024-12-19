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
    private readonly MemoryCacheEntryOptions _cacheEntryOptions;
    private readonly ISerializer _serializer;
    private readonly KeyLocker _locker;
    private readonly IConfiguration _configuration;
    private readonly IProxyConfigProvider _proxyConfigProvider;
    private readonly ConfigSettings _config;
    private readonly ILogger<RouteRunner> _logger;

    public RouteRunner(IServiceProvider serviceProvider, RedisClient redis, IMemoryCache cache, MemoryCacheEntryOptions cacheEntryOptions, ISerializer serializer, KeyLocker locker, IProxyConfigProvider proxyConfigProvider, IOptions<ConfigSettings> config, IConfiguration configuration, ILogger<RouteRunner> logger)
    {
        _serviceProvider = serviceProvider;
        _redis = redis;
        _cache = cache;
        _cacheEntryOptions = cacheEntryOptions;
        _serializer = serializer;
        _locker = locker;
        _configuration = configuration;
        _proxyConfigProvider = proxyConfigProvider;
        _config = config.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(AuthWorker worker, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{Message}", nameof(RouteRunner));

        _subscriber?.Dispose();

        _subscriber = _redis.Subscribe(_config.Queues[ConfigSettings.QUEUES_ROUTE_CHANGE_EVENT], Handler);

        await UpdateRouteAsync(null, cancellationToken);

        return;

        async void Handler(string s, object o)
        {
            await UpdateRouteAsync(o.ToString(), cancellationToken);
        }
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

            await _locker.WaitAsync(nameof(UpdateRouteAsync));

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

            _cache.Set(ConfigSettings.CACHE_ROUTE, cacheRouteGroups, _cacheEntryOptions);
            _cache.Set(ConfigSettings.CACHE_ROUTE_CHECK_KEY, cacheRouteGroups.Count);

            var gatewayConfig = _configuration.GetSection("ReverseProxy");

            if (gatewayConfig.Exists())
                return;

            var gatewayUrl = _config.Services["Netcorext.Auth.Gateway"].Url;

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
                                                                                       Address = gatewayUrl
                                                                                   }
                                                                               }
                                                                           }
                                                        })
                                           .ToArray();

            var routes = cacheRouteGroups.Values
                                         .SelectMany(t => t.Routes
                                                           .Select(t2 => new { t2.Protocol, t2.HttpMethod, t2.RelativePath })
                                                           .Distinct()
                                                           .GroupBy(t2 => new { t2.Protocol, t2.RelativePath }, t2 => t2.HttpMethod)
                                                           .Select(t2 => new RouteConfig
                                                                         {
                                                                             ClusterId = $"{t.Id}-{t.Name}",
                                                                             RouteId = $"{t2.Key.Protocol} - {t.BaseUrl}/{t2.Key.RelativePath}",
                                                                             Match = new RouteMatch
                                                                                     {
                                                                                         Methods = t2.ToArray(),
                                                                                         Path = t2.Key.RelativePath
                                                                                     }
                                                                         }))
                                         .ToArray();

            (_proxyConfigProvider as InMemoryConfigProvider)?.Update(routes, clusters);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
        }
        finally
        {
            _locker.Release(nameof(UpdateRouteAsync));
        }
    }

    public void Dispose()
    {
        _subscriber?.Dispose();
    }
}

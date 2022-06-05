using System.Text.Json;
using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Services.Route;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Contracts;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;
using Netcorext.Worker;
using Yarp.ReverseProxy.Configuration;

namespace Netcorext.Auth.Authentication.Workers;

internal class RouteRunner : IWorkerRunner<AuthWorker>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RedisClient _redis;
    private readonly IMemoryCache _cache;
    private readonly InMemoryConfigProvider _memoryConfigProvider;
    private readonly ConfigSettings _config;
    private readonly ILogger<RouteRunner> _logger;
    private IDisposable? _subscription;
    private static readonly SemaphoreSlim RouteUpdateLocker = new(1, 1);

    public RouteRunner(IServiceProvider serviceProvider, RedisClient redis, IMemoryCache cache, IProxyConfigProvider proxyConfigProvider, IOptions<ConfigSettings> config, ILogger<RouteRunner> logger)
    {
        _serviceProvider = serviceProvider;
        _redis = redis;
        _cache = cache;
        _memoryConfigProvider = (InMemoryConfigProvider)proxyConfigProvider;
        _config = config.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(AuthWorker worker, CancellationToken cancellationToken = default)
    {
        _subscription?.Dispose();

        _subscription = _redis.Subscribe(_config.Queues[ConfigSettings.QUEUES_ROUTE_CHANGE_EVENT], (s, o) => UpdateRouteAsync(o.ToString(), cancellationToken).GetAwaiter().GetResult());

        await UpdateRouteAsync(null, cancellationToken);
    }

    private async Task UpdateRouteAsync(string? ids, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(nameof(UpdateRouteAsync));

            using var scope = _serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            var request = new GetRoute
                          {
                              GroupIds = ids == null ? null : JsonSerializer.Deserialize<long[]>(ids)
                          };

            var result = await dispatcher.SendAsync(request, cancellationToken);

            await RouteUpdateLocker.WaitAsync(cancellationToken);

            if (result?.Content == null || result.Code != Result.Success)
                return;

            var dRouteGroups = _cache.Get<Dictionary<long, Services.Route.Models.RouteGroup>>(ConfigSettings.CACHE_ROUTE) ?? new Dictionary<long, Services.Route.Models.RouteGroup>();

            foreach (var group in result.Content)
            {
                if (dRouteGroups.TryAdd(group.Id, group))
                    continue;

                dRouteGroups[group.Id] = group;
            }

            if (request.GroupIds != null)
            {
                var diffIds = request.GroupIds.ExceptBoth(result.Content.Select(t => t.Id));

                foreach (var id in diffIds.First)
                {
                    dRouteGroups.Remove(id);
                }
            }

            _cache.Set(ConfigSettings.CACHE_ROUTE, dRouteGroups);

            var clusters = dRouteGroups.Values
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

            var routes = dRouteGroups.Values
                                     .SelectMany(t => t.Routes.Select(t2 => new RouteConfig
                                                                            {
                                                                                ClusterId = $"{t.Id}-{t.Name}",
                                                                                RouteId = $"{t2.Protocol} - {t2.HttpMethod} {t.BaseUrl}/{t2.RelativePath}",
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
        _subscription?.Dispose();
    }
}
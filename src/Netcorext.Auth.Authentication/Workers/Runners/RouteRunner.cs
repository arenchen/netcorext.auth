using System.Text.Json;
using FreeRedis;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Services.Route;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Auth.Helpers;
using Netcorext.Contracts;
using Netcorext.Mediator;
using Netcorext.Worker;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

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
            await RouteUpdateLocker.WaitAsync(cancellationToken);

            _logger.LogInformation(nameof(UpdateRouteAsync));

            using var scope = _serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            var result = await dispatcher.SendAsync(new GetRoute
                                                    {
                                                        Ids = ids == null ? null : JsonSerializer.Deserialize<long[]>(ids)
                                                    }, cancellationToken);

            if (result?.Content == null || result.Code != Result.Success) return;

            var dRoute = _cache.Get<Dictionary<long, Services.Route.Models.Route>>(ConfigSettings.CACHE_ROUTE) ?? new Dictionary<long, Services.Route.Models.Route>();

            foreach (var route in result.Content)
            {
                if (dRoute.TryAdd(route.Id, route)) continue;

                dRoute[route.Id] = route;
            }

            _cache.Set(ConfigSettings.CACHE_ROUTE, dRoute);

            var clusters = dRoute.Values
                                 .Select(t => new { t.Group, t.Protocol, t.BaseUrl })
                                 .GroupBy(t => new { t.Group, t.Protocol },
                                          (group, urls) => new
                                                           {
                                                               group.Group,
                                                               group.Protocol,
                                                               Urls = urls.Distinct().ToArray()
                                                           })
                                 .Select(t => new ClusterConfig
                                              {
                                                  ClusterId = $"{t.Group} - {t.Protocol}",
                                                  HttpRequest = _config.AppSettings.ForwarderRequestConfig,
                                                  // HttpRequest = t.Protocol == HttpProtocols.Http2.ToString().ToUpper()
                                                  //                   ? new ForwarderRequestConfig
                                                  //                     {
                                                  //                         Version = new Version(2, 0),
                                                  //                         VersionPolicy = HttpVersionPolicy.RequestVersionExact
                                                  //                     }
                                                  //                   : null,
                                                  Destinations = t.Urls
                                                                  .Select(t2 => new
                                                                                {
                                                                                    Name = $"{t.Protocol} - {UrlHelper.GetHostAndPort(t2.BaseUrl)}",
                                                                                    Config = new DestinationConfig
                                                                                             {
                                                                                                 Address = t2.BaseUrl
                                                                                             }
                                                                                })
                                                                  .ToDictionary(t2 => t2.Name, t2 => t2.Config)
                                              })
                                 .ToArray();

            var routes = dRoute.Values
                               .Select(t => new RouteConfig
                                            {
                                                RouteId = $"{t.Protocol} - {t.HttpMethod} {t.BaseUrl}/{t.RelativePath}",
                                                ClusterId = $"{t.Group} - {t.Protocol}",
                                                Match = new RouteMatch
                                                        {
                                                            Path = t.RelativePath,
                                                            Hosts = new[] { t.Protocol == HttpProtocols.Http2.ToString().ToUpper() ? _config.AppSettings.Http2HostMatchPattern : _config.AppSettings.HttpHostMatchPattern }
                                                        }
                                            })
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
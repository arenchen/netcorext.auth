using System.Text.Json;
using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Services.Permission;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Contracts;
using Netcorext.Mediator;
using Netcorext.Worker;

namespace Netcorext.Auth.Authentication.Workers;

internal class RoleRunner : IWorkerRunner<AuthWorker>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RedisClient _redis;
    private readonly IMemoryCache _cache;
    private readonly ConfigSettings _config;
    private readonly ILogger<RoleRunner> _logger;
    private IDisposable? _subscription;
    private static readonly SemaphoreSlim RoleUpdateLocker = new(1, 1);

    public RoleRunner(IServiceProvider serviceProvider, RedisClient redis, IMemoryCache cache, IOptions<ConfigSettings> config, ILogger<RoleRunner> logger)
    {
        _serviceProvider = serviceProvider;
        _redis = redis;
        _cache = cache;
        _config = config.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(AuthWorker worker, CancellationToken cancellationToken = default)
    {
        _subscription?.Dispose();

        _subscription = _redis.Subscribe(_config.Queues[ConfigSettings.QUEUES_ROLE_CHANGE_EVENT], (s, o) => UpdateRoleAsync(o.ToString(), cancellationToken).GetAwaiter().GetResult());

        await UpdateRoleAsync(null, cancellationToken);
    }

    private async Task UpdateRoleAsync(string? ids, CancellationToken cancellationToken = default)
    {
        try
        {
            await RoleUpdateLocker.WaitAsync(cancellationToken);

            _logger.LogInformation(nameof(UpdateRoleAsync));

            using var scope = _serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            var result = await dispatcher.SendAsync(new GetRolePermission
                                                    {
                                                        Ids = ids == null ? null : JsonSerializer.Deserialize<long[]>(ids)
                                                    }, cancellationToken);

            if (result?.Content == null || result.Code != Result.Success) return;

            var cachePermissions = _cache.Get<Dictionary<long, Services.Permission.Models.Permission>>(ConfigSettings.CACHE_ROLE_PERMISSION) ?? new Dictionary<long, Services.Permission.Models.Permission>();

            foreach (var permission in result.Content)
            {
                if (permission.Disabled)
                {
                    cachePermissions.Remove(permission.Id);

                    continue;
                }

                if (cachePermissions.TryAdd(permission.Id, permission)) continue;
                
                cachePermissions[permission.Id] = permission;
            }
            
            _cache.Set(ConfigSettings.CACHE_ROLE_PERMISSION, cachePermissions);
        }
        finally
        {
            RoleUpdateLocker.Release();
        }
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
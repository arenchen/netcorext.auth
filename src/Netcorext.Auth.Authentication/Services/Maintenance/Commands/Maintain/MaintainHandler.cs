using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Maintenance.Commands;

public class MaintainHandler : IRequestHandler<Maintain, Result>
{
    private readonly IMemoryCache _cache;
    private readonly RedisClient _redis;
    private readonly ConfigSettings _config;

    public MaintainHandler(IMemoryCache cache, RedisClient redis, IOptions<ConfigSettings> config)
    {
        _cache = cache;
        _redis = redis;
        _config = config.Value;
    }

    public async Task<Result> Handle(Maintain request, CancellationToken cancellationToken = default)
    {
        var key = _config.Caches[ConfigSettings.CACHE_MAINTAIN_KEY].Key;

        _cache.Set(ConfigSettings.CACHE_MAINTAIN_KEY, request);

        await _redis.SetAsync(key, request);

        return Result.Success;
    }
}
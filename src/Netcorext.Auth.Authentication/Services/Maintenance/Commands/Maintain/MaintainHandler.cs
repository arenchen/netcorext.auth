using FreeRedis;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Maintenance.Commands;

public class MaintainHandler : IRequestHandler<Maintain, Result>
{
    private readonly RedisClient _redis;
    private readonly ConfigSettings _config;

    public MaintainHandler(RedisClient redis, IOptions<ConfigSettings> config)
    {
        _redis = redis;
        _config = config.Value;
    }

    public async Task<Result> Handle(Maintain request, CancellationToken cancellationToken = default)
    {
        var cacheData = _config.Caches[ConfigSettings.CACHE_MAINTAIN];

        await _redis.SetAsync(cacheData.Key, request);

        await _redis.PublishAsync(_config.Queues[ConfigSettings.QUEUES_MAINTAIN_CHANGE_EVENT], Array.Empty<byte>());

        return Result.Success;
    }
}

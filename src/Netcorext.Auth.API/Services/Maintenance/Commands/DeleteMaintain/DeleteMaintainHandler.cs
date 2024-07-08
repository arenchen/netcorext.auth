using Netcorext.Contracts;
using Netcorext.Mediator;
using FreeRedis;
using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Settings;

namespace Netcorext.Auth.API.Services.Maintenance.Commands;

public class DeleteMaintainHandler : IRequestHandler<DeleteMaintain, Result>
{
    private readonly RedisClient _redis;
    private readonly ConfigSettings _config;

    public DeleteMaintainHandler(RedisClient redis, IOptions<ConfigSettings> config)
    {
        _redis = redis;
        _config = config.Value;
    }

    public async Task<Result> Handle(DeleteMaintain request, CancellationToken cancellationToken = default)
    {
        var cacheData = _config.Caches[ConfigSettings.CACHE_MAINTAIN];
        var keys = new HashSet<string>();

        foreach (var key in request.Keys)
        {
            keys.Add(key.ToLower());
            await _redis.HDelAsync(cacheData.Key, key);
        }

        await _redis.PublishAsync(_config.Queues[ConfigSettings.QUEUES_MAINTAIN_CHANGE_EVENT], $"[{string.Join(',', keys)}]");

        return Result.Success;
    }
}

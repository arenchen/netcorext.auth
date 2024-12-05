using Netcorext.Contracts;
using Netcorext.Mediator;
using FreeRedis;
using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Settings;

namespace Netcorext.Auth.API.Services.Maintenance.Commands;

public class UpdateMaintainHandler : IRequestHandler<UpdateMaintain, Result>
{
    private readonly RedisClient _redis;
    private readonly ConfigSettings _config;

    public UpdateMaintainHandler(RedisClient redis, IOptions<ConfigSettings> config)
    {
        _redis = redis;
        _config = config.Value;
    }

    public async Task<Result> Handle(UpdateMaintain request, CancellationToken cancellationToken = default)
    {
        var cacheData = _config.Caches[ConfigSettings.CACHE_MAINTAIN];

        var keys = request.Items.Keys.Select(x => x.ToLower()).Distinct().ToArray();

        await _redis.HSetAsync(cacheData.Key, request.Items);

        await _redis.PublishAsync(_config.Queues[ConfigSettings.QUEUES_MAINTAIN_CHANGE_EVENT], $"[{string.Join(',', keys)}]");

        return Result.Success;
    }
}

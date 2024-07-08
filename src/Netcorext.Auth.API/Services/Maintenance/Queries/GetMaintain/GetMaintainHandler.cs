using Netcorext.Contracts;
using Netcorext.Mediator;
using FreeRedis;
using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Settings;

namespace Netcorext.Auth.API.Services.Maintenance.Queries;

public class GetMaintainHandler : IRequestHandler<GetMaintain, Result<Dictionary<string, Models.MaintainItem>>>
{
    private readonly RedisClient _redis;
    private readonly ConfigSettings _config;

    public GetMaintainHandler(RedisClient redis, IOptions<ConfigSettings> config)
    {
        _redis = redis;
        _config = config.Value;
    }

    public async Task<Result<Dictionary<string, Models.MaintainItem>>> Handle(GetMaintain request, CancellationToken cancellationToken = default)
    {
        var cfgCache = _config.Caches[ConfigSettings.CACHE_MAINTAIN];

        var data = await _redis.HGetAllAsync<Models.MaintainItem>(cfgCache.Key);

        return Result<Dictionary<string, Models.MaintainItem>>.Success.Clone(data);
    }
}

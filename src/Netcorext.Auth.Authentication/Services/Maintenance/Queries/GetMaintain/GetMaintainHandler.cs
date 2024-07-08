using FreeRedis;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Maintenance.Queries;

public class GetMaintainHandler : IRequestHandler<GetMaintain, Result<IDictionary<string, Models.MaintainItem>>>
{
    private readonly RedisClient _redis;
    private readonly ConfigSettings _config;

    public GetMaintainHandler(RedisClient redis, IOptions<ConfigSettings> config)
    {
        _redis = redis;
        _config = config.Value;
    }

    public async Task<Result<IDictionary<string, Models.MaintainItem>>> Handle(GetMaintain request, CancellationToken cancellationToken = new CancellationToken())
    {
        var cfgCache = _config.Caches[ConfigSettings.CACHE_MAINTAIN];

        var data = await _redis.HGetAllAsync<Models.MaintainItem>(cfgCache.Key);

        return Result<IDictionary<string, Models.MaintainItem>>.Success.Clone(data);
    }
}

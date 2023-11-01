using FreeRedis;
using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Services.Blocked.Commands;
using Netcorext.Auth.API.Settings;
using Netcorext.Contracts;
using Netcorext.Mediator.Pipelines;
using Netcorext.Serialization;

namespace Netcorext.Auth.API.Services.Blocked.Pipelines;

public class BlockedIpChangeNotifyPipeline : IRequestPipeline<CreateBlockedIp, Result<IEnumerable<long>>>,
                                             IRequestPipeline<UpdateBlockedIp, Result>,
                                             IRequestPipeline<DeleteBlockedIp, Result>
{
    private readonly RedisClient _redis;
    private readonly ISerializer _serializer;
    private readonly ConfigSettings _config;

    public BlockedIpChangeNotifyPipeline(RedisClient redis, ISerializer serializer, IOptions<ConfigSettings> config)
    {
        _redis = redis;
        _serializer = serializer;
        _config = config.Value;
    }

    public async Task<Result<IEnumerable<long>>?> InvokeAsync(CreateBlockedIp request, PipelineDelegate<Result<IEnumerable<long>>> next, CancellationToken cancellationToken = default)
    {
        var result = await next(request, cancellationToken);

        if (result == Result.SuccessCreated && result.Content != null)
            await NotifyAsync(result.Content.ToArray());

        return result;
    }

    public async Task<Result?> InvokeAsync(UpdateBlockedIp request, PipelineDelegate<Result> next, CancellationToken cancellationToken = default)
    {
        var result = await next(request, cancellationToken);

        if (result == Result.SuccessNoContent)
            await NotifyAsync(request.Id);

        return result;
    }

    public async Task<Result?> InvokeAsync(DeleteBlockedIp request, PipelineDelegate<Result> next, CancellationToken cancellationToken = default)
    {
        var result = await next(request, cancellationToken);

        if (result == Result.SuccessNoContent)
            await NotifyAsync(request.Ids);

        return result;
    }

    private async Task NotifyAsync(params long[] ids)
    {
        var value = await _serializer.SerializeAsync(ids);

        await _redis.PublishAsync(_config.Queues[ConfigSettings.QUEUES_BLOCKED_IP_CHANGE_EVENT], value);
    }
}

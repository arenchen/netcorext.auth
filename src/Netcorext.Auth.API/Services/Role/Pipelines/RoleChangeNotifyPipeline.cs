using FreeRedis;
using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Services.Role.Commands;
using Netcorext.Auth.API.Settings;
using Netcorext.Contracts;
using Netcorext.Mediator.Pipelines;
using Netcorext.Serialization;

namespace Netcorext.Auth.API.Services.Role.Pipelines;

public class RoleChangeNotifyPipeline : IRequestPipeline<CreateRole, Result<IEnumerable<long>>>,
                                        IRequestPipeline<UpdateRole, Result>,
                                        IRequestPipeline<DeleteRole, Result>
{
    private readonly ISerializer _serializer;
    private readonly RedisClient _redis;
    private readonly ConfigSettings _config;

    public RoleChangeNotifyPipeline(RedisClient redis, ISerializer serializer, IOptions<ConfigSettings> config)
    {
        _serializer = serializer;
        _redis = redis;
        _config = config.Value;
    }

    public async Task<Result<IEnumerable<long>>?> InvokeAsync(CreateRole request, PipelineDelegate<Result<IEnumerable<long>>> next, CancellationToken cancellationToken = default)
    {
        var result = await next(request, cancellationToken);

        if (result == Result.SuccessCreated && result.Content != null)
            await NotifyAsync(result.Content.ToArray());

        return result;
    }

    public async Task<Result?> InvokeAsync(UpdateRole request, PipelineDelegate<Result> next, CancellationToken cancellationToken = default)
    {
        var result = await next(request, cancellationToken);

        if (result == Result.SuccessNoContent)
            await NotifyAsync(request.Id);

        return result;
    }

    public async Task<Result?> InvokeAsync(DeleteRole request, PipelineDelegate<Result> next, CancellationToken cancellationToken = default)
    {
        var result = await next(request, cancellationToken);

        if (result == Result.SuccessNoContent)
            await NotifyAsync(request.Ids);

        return result;
    }

    private async Task NotifyAsync(params long[] ids)
    {
        var value = await _serializer.SerializeAsync(ids);

        await _redis.PublishAsync(_config.Queues[ConfigSettings.QUEUES_ROLE_CHANGE_EVENT], value);
    }
}
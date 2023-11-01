using FreeRedis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Services.Permission.Commands;
using Netcorext.Auth.API.Settings;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator.Pipelines;
using Netcorext.Serialization;

namespace Netcorext.Auth.API.Services.Permission.Pipelines;

public class PermissionChangeNotifyPipeline : IRequestPipeline<CreatePermission, Result<IEnumerable<long>>>,
                                              IRequestPipeline<UpdatePermission, Result>,
                                              IRequestPipeline<DeletePermission, Result>
{
    private readonly ISerializer _serializer;
    private readonly RedisClient _redis;
    private readonly ConfigSettings _config;

    public PermissionChangeNotifyPipeline(RedisClient redis, ISerializer serializer, IOptions<ConfigSettings> config)
    {
        _redis = redis;
        _serializer = serializer;
        _config = config.Value;
    }

    public async Task<Result<IEnumerable<long>>?> InvokeAsync(CreatePermission request, PipelineDelegate<Result<IEnumerable<long>>> next, CancellationToken cancellationToken = default)
    {
        var result = await next(request, cancellationToken);

        if (result == Result.SuccessCreated && result.Content != null)
            await NotifyAsync(result.Content.ToArray());

        return result;
    }

    public async Task<Result?> InvokeAsync(UpdatePermission request, PipelineDelegate<Result> next, CancellationToken cancellationToken = default)
    {
        var result = await next(request, cancellationToken);

        if (result == Result.SuccessNoContent)
            await NotifyAsync(request.Id);

        return result;
    }

    public async Task<Result?> InvokeAsync(DeletePermission request, PipelineDelegate<Result> next, CancellationToken cancellationToken = default)
    {
        var result = await next(request, cancellationToken);

        if (result == Result.SuccessNoContent)
            await NotifyAsync(request.Ids);

        return result;
    }

    private async Task NotifyAsync(params long[] ids)
    {
        var value = await _serializer.SerializeAsync(ids);

        await _redis.PublishAsync(_config.Queues[ConfigSettings.QUEUES_PERMISSION_CHANGE_EVENT], value);
    }
}

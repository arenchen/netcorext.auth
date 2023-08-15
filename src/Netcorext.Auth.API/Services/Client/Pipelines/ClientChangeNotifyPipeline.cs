using FreeRedis;
using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Services.Client.Commands;
using Netcorext.Auth.API.Settings;
using Netcorext.Contracts;
using Netcorext.Mediator.Pipelines;
using Netcorext.Serialization;

namespace Netcorext.Auth.API.Services.Client.Pipelines;

public class ClientChangeNotifyPipeline : IRequestPipeline<CreateClient, Result<long?>>,
                                          IRequestPipeline<UpdateClient, Result>,
                                          IRequestPipeline<DeleteClient, Result>
{
    private readonly ISerializer _serializer;
    private readonly RedisClient _redis;
    private readonly ConfigSettings _config;

    public ClientChangeNotifyPipeline(RedisClient redis, ISerializer serializer, IOptions<ConfigSettings> config)
    {
        _serializer = serializer;
        _redis = redis;
        _config = config.Value;
    }

    public async Task<Result<long?>?> InvokeAsync(CreateClient request, PipelineDelegate<Result<long?>> next, CancellationToken cancellationToken = default)
    {
        var result = await next(request, cancellationToken);

        if (result == Result.SuccessCreated && result.Content.HasValue)
            await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_CLIENT_CHANGE_EVENT], result.Content.Value);

        if (result == Result.SuccessCreated && result.Content.HasValue && (request.Roles ?? Array.Empty<CreateClient.ClientRole>()).Any())
            await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_CLIENT_ROLE_CHANGE_EVENT], result.Content.Value);

        return result;
    }

    public async Task<Result?> InvokeAsync(UpdateClient request, PipelineDelegate<Result> next, CancellationToken cancellationToken = default)
    {
        var result = await next(request, cancellationToken);

        if (result == Result.SuccessNoContent)
            await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_CLIENT_CHANGE_EVENT], request.Id);

        if (result == Result.SuccessNoContent && (request.Roles ?? Array.Empty<UpdateClient.ClientRole>()).Any())
            await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_CLIENT_ROLE_CHANGE_EVENT], request.Id);

        return result;
    }

    public async Task<Result?> InvokeAsync(DeleteClient request, PipelineDelegate<Result> next, CancellationToken cancellationToken = default)
    {
        var result = await next(request, cancellationToken);

        if (result != Result.SuccessNoContent) return result;

        await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_CLIENT_ROLE_CHANGE_EVENT], request.Id);
        await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_CLIENT_CHANGE_EVENT], request.Id);

        return result;
    }

    private async Task NotifyAsync(string channelId, params long[] ids)
    {
        var value = await _serializer.SerializeAsync(ids);

        await _redis.PublishAsync(channelId, value);
    }
}
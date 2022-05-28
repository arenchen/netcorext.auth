using System.Text.Json;
using FreeRedis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Settings;
using Netcorext.Contracts;
using Netcorext.Mediator.Pipelines;

namespace Netcorext.Auth.API.Services.Client.Pipelines;

public class ClientChangeNotifyPipeline : IRequestPipeline<CreateClient, Result<long?>>,
                                          IRequestPipeline<UpdateClient, Result>,
                                          IRequestPipeline<DeleteClient, Result>
{
    private readonly RedisClient _redis;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ConfigSettings _config;

    public ClientChangeNotifyPipeline(RedisClient redis, IOptions<ConfigSettings> config, IOptions<JsonOptions> jsonOptions)
    {
        _redis = redis;
        _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
        _config = config.Value;
    }
    
    public async Task<Result<long?>?> InvokeAsync(CreateClient request, PipelineDelegate<Result<long?>> next, CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await next(request, cancellationToken);
        
        if (result == Result.Success && result.Content.HasValue)
            await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_CLIENT_CHANGE_EVENT], result.Content.Value);
        
        if (result == Result.Success && result.Content.HasValue && (request.Roles ?? Array.Empty<CreateClient.ClientRole>()).Any())
            await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_CLIENT_ROLE_CHANGE_EVENT], result.Content.Value);

        return result;
    }

    public async Task<Result?> InvokeAsync(UpdateClient request, PipelineDelegate<Result> next, CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await next(request, cancellationToken);
        
        if (result == Result.Success)
            await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_CLIENT_CHANGE_EVENT], request.Id);
        
        if (result == Result.Success && (request.Roles ?? Array.Empty<UpdateClient.ClientRole>()).Any())
            await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_CLIENT_ROLE_CHANGE_EVENT], request.Id);

        return result;
    }

    public async Task<Result?> InvokeAsync(DeleteClient request, PipelineDelegate<Result> next, CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await next(request, cancellationToken);

        if (result != Result.Success) return result;

        await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_CLIENT_ROLE_CHANGE_EVENT], request.Id);
        await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_CLIENT_CHANGE_EVENT], request.Id);

        return result;
    }
    
    private Task NotifyAsync(string channelId, params long[] ids)
    {
        var value = JsonSerializer.Serialize(ids, _jsonOptions);
        
        _redis.Publish(channelId, value);

        return Task.CompletedTask;
    }
}
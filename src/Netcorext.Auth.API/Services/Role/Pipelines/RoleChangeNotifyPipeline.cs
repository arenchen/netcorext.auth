using System.Text.Json;
using FreeRedis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Settings;
using Netcorext.Contracts;
using Netcorext.Mediator.Pipelines;

namespace Netcorext.Auth.API.Services.Role.Pipelines;

public class RoleChangeNotifyPipeline : IRequestPipeline<CreateRole, Result<IEnumerable<long>>>,
                                        IRequestPipeline<UpdateRole, Result>,
                                        IRequestPipeline<DeleteRole, Result>
{
    private readonly RedisClient _redis;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ConfigSettings _config;

    public RoleChangeNotifyPipeline(RedisClient redis, IOptions<ConfigSettings> config, IOptions<JsonOptions> jsonOptions)
    {
        _redis = redis;
        _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
        _config = config.Value;
    }
    
    public async Task<Result<IEnumerable<long>>?> InvokeAsync(CreateRole request, PipelineDelegate<Result<IEnumerable<long>>> next, CancellationToken cancellationToken = default)
    {
        var result = await next(request, cancellationToken);
        
        if (result == Result.Success && result.Content != null)
            await NotifyAsync(result.Content.ToArray());

        return result;
    }

    public async Task<Result?> InvokeAsync(UpdateRole request, PipelineDelegate<Result> next, CancellationToken cancellationToken = default)
    {
        var result = await next(request, cancellationToken);
        
        if (result == Result.Success)
            await NotifyAsync(request.Id);

        return result;
    }

    public async Task<Result?> InvokeAsync(DeleteRole request, PipelineDelegate<Result> next, CancellationToken cancellationToken = default)
    {
        var result = await next(request, cancellationToken);
        
        if (result == Result.Success)
            await NotifyAsync(request.Ids);

        return result;
    }

    private Task NotifyAsync(params long[] ids)
    {
        var value = JsonSerializer.Serialize(ids, _jsonOptions);
        
        _redis.Publish(_config.Queues[ConfigSettings.QUEUES_ROLE_CHANGE_EVENT], value);

        return Task.CompletedTask;
    }
}
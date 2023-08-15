using FreeRedis;
using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Services.User.Commands;
using Netcorext.Auth.API.Settings;
using Netcorext.Contracts;
using Netcorext.Mediator.Pipelines;
using Netcorext.Serialization;

namespace Netcorext.Auth.API.Services.User.Pipelines;

public class UserChangeNotifyPipeline : IRequestPipeline<CreateUser, Result<long?>>,
                                        IRequestPipeline<UpdateUser, Result>,
                                        IRequestPipeline<DeleteUser, Result>
{
    private readonly ISerializer _serializer;
    private readonly RedisClient _redis;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ConfigSettings _config;

    public UserChangeNotifyPipeline(RedisClient redis, IHttpContextAccessor httpContextAccessor, ISerializer serializer, IOptions<ConfigSettings> config)
    {
        _serializer = serializer;
        _redis = redis;
        _httpContextAccessor = httpContextAccessor;
        _config = config.Value;
    }

    public async Task<Result<long?>?> InvokeAsync(CreateUser request, PipelineDelegate<Result<long?>> next, CancellationToken cancellationToken = default)
    {
        var result = await next(request, cancellationToken);

        if (result == Result.SuccessCreated && result.Content.HasValue)
            await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_USER_CHANGE_EVENT], result.Content.Value);

        if (result == Result.SuccessCreated && result.Content.HasValue && (request.Roles ?? Array.Empty<CreateUser.UserRole>()).Any())
            await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_USER_ROLE_CHANGE_EVENT], result.Content.Value);

        return result;
    }

    public async Task<Result?> InvokeAsync(UpdateUser request, PipelineDelegate<Result> next, CancellationToken cancellationToken = default)
    {
        var result = await next(request, cancellationToken);

        if (result == Result.SuccessNoContent)
            await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_USER_CHANGE_EVENT], request.Id);

        if (result == Result.SuccessNoContent && (request.Roles ?? Array.Empty<UpdateUser.UserRole>()).Any())
            await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_USER_ROLE_CHANGE_EVENT], request.Id);

        if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Items.TryGetValue(ConfigSettings.QUEUES_TOKEN_REVOKE_EVENT, out var tokens))
            await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_TOKEN_REVOKE_EVENT], tokens as string[] ?? Array.Empty<string>());

        return result;
    }

    public async Task<Result?> InvokeAsync(DeleteUser request, PipelineDelegate<Result> next, CancellationToken cancellationToken = default)
    {
        var result = await next(request, cancellationToken);

        if (result != Result.SuccessNoContent) return result;

        await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_USER_ROLE_CHANGE_EVENT], request.Id);
        await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_USER_CHANGE_EVENT], request.Id);

        return result;
    }

    private async Task NotifyAsync(string channelId, params long[] values)
    {
        var value = await _serializer.SerializeAsync(values);

        await _redis.PublishAsync(channelId, value);
    }

    private async Task NotifyAsync(string channelId, params string[] values)
    {
        var value = await _serializer.SerializeAsync(values);

        await _redis.PublishAsync(channelId, value);
    }
}
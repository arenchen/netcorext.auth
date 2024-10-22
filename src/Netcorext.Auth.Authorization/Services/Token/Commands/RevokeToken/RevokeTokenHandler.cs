using System.Linq.Expressions;
using FreeRedis;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authorization.Settings;
using Netcorext.Auth.Enums;
using Netcorext.Auth.Helpers;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.EntityFramework.UserIdentityPattern.Extensions;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;
using Netcorext.Serialization;

namespace Netcorext.Auth.Authorization.Services.Token.Commands;

public class RevokeTokenHandler : IRequestHandler<RevokeToken, Result>
{
    private readonly DatabaseContext _context;
    private readonly RedisClient _redis;
    private readonly ISerializer _serializer;
    private readonly ConfigSettings _config;

    public RevokeTokenHandler(DatabaseContextAdapter context, RedisClient redis, ISerializer serializer, IOptions<ConfigSettings> config)
    {
        _context = context;
        _serializer = serializer;
        _redis = redis;
        _config = config.Value;
    }

    public async Task<Result> Handle(RevokeToken request, CancellationToken cancellationToken = default)
    {
        var dsToken = _context.Set<Domain.Entities.Token>();

        Expression<Func<Domain.Entities.Token, bool>> predicate = t => t.Revoked != TokenRevoke.Both;

        var resourceId = TokenHelper.GetResourceId(request.Token) ?? request.ResourceId;

        if (!request.ResourceId.IsEmpty())
            predicate = predicate.And(t => t.ResourceId == request.ResourceId);

        if (!request.Token.IsEmpty())
            predicate = predicate.And(t => t.AccessToken == request.Token || t.RefreshToken == request.Token);

        if (!resourceId.IsEmpty() && request.AllDevice.HasValue && request.AllDevice.Value)
            predicate = predicate.And(t => t.ResourceId == resourceId);

        var tokens = dsToken.Where(predicate)
                            .ToArray();

        if (!tokens.Any())
            return Result.Success;

        var lsToken = new List<string>();

        foreach (var i in tokens)
        {
            _context.Entry(i).UpdateProperty(t => t.Revoked, TokenRevoke.Both);

            lsToken.Add(i.AccessToken);

            if (!i.RefreshToken.IsEmpty())
                lsToken.Add(i.RefreshToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (!resourceId.IsEmpty())
        {
            var signature = $"user:{resourceId}";

            if (_config.Caches.TryGetValue(ConfigSettings.CACHE_TOKEN_RETAIN, out var cache) && !cache.Key.IsEmpty() && cache.ServerDuration is > 0 && !signature.IsEmpty())
            {
                await _redis.DelAsync(cache.Key + ":" + signature);
            }
        }

        await _redis.PublishAsync(_config.Queues[ConfigSettings.QUEUES_TOKEN_REVOKE_EVENT], await _serializer.SerializeAsync(lsToken.ToArray(), cancellationToken));

        return Result.Success;
    }
}

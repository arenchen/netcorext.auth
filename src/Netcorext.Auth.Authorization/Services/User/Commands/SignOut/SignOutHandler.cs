using System.Linq.Expressions;
using FreeRedis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authorization.Settings;
using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.EntityFramework.UserIdentityPattern.Extensions;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;
using Netcorext.Serialization;

namespace Netcorext.Auth.Authorization.Services.User.Commands;

public class SignOutHandler : IRequestHandler<SignOut, Result>
{
    private readonly DatabaseContext _context;
    private readonly ISerializer _serializer;
    private readonly RedisClient _redis;
    private readonly ConfigSettings _config;

    public SignOutHandler(DatabaseContextAdapter context, RedisClient redis, ISerializer serializer, IOptions<ConfigSettings> config)
    {
        _context = context;
        _serializer = serializer;
        _redis = redis;
        _config = config.Value;
    }

    public async Task<Result> Handle(SignOut request, CancellationToken cancellationToken = default)
    {
        var dsToken = _context.Set<Domain.Entities.Token>();

        var token = await dsToken.FirstOrDefaultAsync(t => t.AccessToken == request.Token, cancellationToken);

        if (token == null)
            return Result.Success;

        Expression<Func<Domain.Entities.Token, bool>> predicate = t => t.Id == token.Id;

        if (request.AllDevice)
            predicate = predicate.Or(t => t.ResourceId == token.ResourceId && t.ResourceType == token.ResourceType);

        var tokens = dsToken.Where(predicate);

        var lsToken = new List<string>();

        foreach (var i in tokens)
        {
            _context.Entry(i).UpdateProperty(t => t.Revoked, TokenRevoke.Both);

            lsToken.Add(i.AccessToken);

            if (!i.RefreshToken.IsEmpty())
                lsToken.Add(i.RefreshToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _redis.PublishAsync(_config.Queues[ConfigSettings.QUEUES_TOKEN_REVOKE_EVENT], await _serializer.SerializeAsync(lsToken.ToArray(), cancellationToken));

        return Result.Success;
    }
}

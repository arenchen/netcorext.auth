using System.Text.Json;
using FreeRedis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authorization.Settings;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.EntityFramework.UserIdentityPattern.Extensions;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authorization.Services.User.Commands;

public class SignOutHandler : IRequestHandler<SignOut, Result>
{
    private readonly DatabaseContext _context;
    private readonly RedisClient _redis;
    private readonly ConfigSettings _config;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public SignOutHandler(DatabaseContext context, RedisClient redis, IOptions<ConfigSettings> config, IOptions<JsonOptions> jsonOptions)
    {
        _context = context;
        _redis = redis;
        _config = config.Value;
        _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
    }

    public async Task<Result> Handle(SignOut request, CancellationToken cancellationToken = default)
    {
        var dsToken = _context.Set<Domain.Entities.Token>();

        if (!await dsToken.AnyAsync(t => t.AccessToken == request.Token || t.RefreshToken == request.Token, cancellationToken)) return Result.Success;

        var token = await dsToken.FirstOrDefaultAsync(t => t.AccessToken == request.Token || t.RefreshToken == request.Token, cancellationToken);

        if (token != null)
            _context.Entry(token).UpdateProperty(t => t.Disabled, true);

        await _context.SaveChangesAsync(cancellationToken);

        if (token == null) return Result.Success;

        var lsToken = new List<string> { token.AccessToken };
                
        if (!string.IsNullOrWhiteSpace(token.RefreshToken)) lsToken.Add(token.RefreshToken);
                
        _redis.Publish(_config.Queues[ConfigSettings.QUEUES_TOKEN_REVOKE_EVENT], JsonSerializer.Serialize(lsToken.ToArray(), _jsonOptions));

        return Result.Success;
    }
}
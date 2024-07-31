using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Netcorext.Auth.Enums;
using Netcorext.Auth.Extensions;
using Netcorext.Auth.Helpers;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Token.Queries;

public class ValidateTokenHandler : IRequestHandler<ValidateToken, Result>
{
    private readonly IMemoryCache _cache;
    private readonly DatabaseContext _context;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public ValidateTokenHandler(DatabaseContextAdapter context, IMemoryCache cache, IOptions<AuthOptions> authOptions)
    {
        _cache = cache;
        _context = context.Slave;
        _tokenValidationParameters = authOptions.Value.GetTokenValidationParameters();
    }

    public async Task<Result> Handle(ValidateToken request, CancellationToken cancellationToken = default)
    {
        if (request.ValidationParameters != null)
        {
            try
            {
                _tokenValidationParameters.ValidateIssuer = request.ValidationParameters.ValidateIssuer;
                _tokenValidationParameters.ValidateAudience = request.ValidationParameters.ValidateAudience;
                _tokenValidationParameters.ValidateLifetime = request.ValidationParameters.ValidateLifetime;
                _tokenValidationParameters.ValidateIssuerSigningKey = request.ValidationParameters.ValidateIssuerSigningKey;

                TokenHelper.ValidateJwt(request.Token, _tokenValidationParameters);
            }
            catch (Exception)
            {
                return Result.Unauthorized;
            }
        }

        var ds = _context.Set<Domain.Entities.Token>();
        var dsUser = _context.Set<Domain.Entities.User>();
        var dsClient = _context.Set<Domain.Entities.Client>();

        var entity = await ds.Select(t => new { t.ResourceType, t.ResourceId, t.AccessToken, t.RefreshToken, t.Revoked })
                             .FirstOrDefaultAsync(t => t.AccessToken == request.Token || t.RefreshToken == request.Token, cancellationToken);

        if (entity == null)
            return Result.Unauthorized;
        if (entity.Revoked == TokenRevoke.Both)
            return Result.UnauthorizedAndCannotRefreshToken;
        if (entity.AccessToken == request.Token && (entity.Revoked & TokenRevoke.AccessToken) == TokenRevoke.AccessToken)
            return Result.Unauthorized;
        if (entity.RefreshToken == request.Token && (entity.Revoked & TokenRevoke.RefreshToken) == TokenRevoke.RefreshToken)
            return Result.Unauthorized;

        var isResourceValid = entity.ResourceType switch
                              {
                                  ResourceType.Client => await dsClient.Select(t => new { t.Id, t.Disabled }).FirstOrDefaultAsync(t => t.Id == long.Parse(entity.ResourceId), cancellationToken: cancellationToken),
                                  ResourceType.User => await dsUser.Select(t => new { t.Id, t.Disabled }).FirstOrDefaultAsync(t => t.Id == long.Parse(entity.ResourceId), cancellationToken),
                                  _ => null
                              };

        return isResourceValid == null
                   ? Result.Unauthorized
                   : isResourceValid.Disabled
                       ? Result.AccountIsDisabled
                       : Result.Success;
    }
}

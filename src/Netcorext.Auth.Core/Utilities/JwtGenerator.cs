using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Enums;
using Netcorext.Auth.Helpers;

namespace Netcorext.Auth.Utilities;

public class JwtGenerator
{
    private readonly AuthOptions _config;

    public JwtGenerator(IOptions<AuthOptions> config)
    {
        _config = config.Value;
    }

    public (string Token, JwtSecurityToken Jwt) Generate(TokenType tokenType, ResourceType resourceType, string resourceId, string? uniqueId = null, int? tokenExpireSeconds = null, string? scope = null, string? originScope = null)
    {
        return TokenHelper.Generate(tokenType,
                                    resourceType,
                                    DateTime.UtcNow.AddSeconds(tokenExpireSeconds ?? (tokenType == TokenType.AccessToken ? _config.TokenExpireSeconds : _config.RefreshTokenExpireSeconds)),
                                    resourceId,
                                    uniqueId,
                                    scope,
                                    _config.Issuer,
                                    _config.Audience,
                                    _config.SigningKey,
                                    _config.NameClaimType,
                                    _config.RoleClaimType,
                                    originScope
                                   );
    }
}
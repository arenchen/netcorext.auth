using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Enums;
using Netcorext.Auth.Helpers;

namespace Netcorext.Auth.Utilities;

public class JwtGenerator
{
    private readonly AuthOptions _config;

    public static readonly (string? Token, JwtSecurityToken? Jwt, int ExpiresIn, long ExpiresAt, string? Signature) DefaultGenerateEmpty = (null, null, 0, 0, null);

    public JwtGenerator(IOptions<AuthOptions> config)
    {
        _config = config.Value;
    }

    public (string Token, JwtSecurityToken Jwt, int ExpiresIn, long ExpiresAt, string Signature) Generate(TokenType tokenType, ResourceType resourceType, string resourceId, string? uniqueId = null, string? nickname = null, int? tokenExpireSeconds = null, string? scope = null, string? label = null)
    {
        return TokenHelper.Generate(tokenType,
                                    resourceType,
                                    DateTimeOffset.UtcNow.AddSeconds(tokenExpireSeconds ?? (tokenType == TokenType.AccessToken ? _config.TokenExpireSeconds : _config.RefreshTokenExpireSeconds)),
                                    resourceId,
                                    uniqueId,
                                    nickname,
                                    scope,
                                    label,
                                    _config.Issuer,
                                    _config.Audience,
                                    _config.SigningKey,
                                    _config.NameClaimType,
                                    _config.RoleClaimType);
    }
}

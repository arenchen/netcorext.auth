using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HashidsNet;
using Microsoft.IdentityModel.Tokens;
using Netcorext.Auth.Enums;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Netcorext.Auth.Helpers;

public static class TokenHelper
{
    public const string CLAIM_TYPES_RESOURCE_TYPE = "rt";
    public const string CLAIM_TYPES_TOKEN_TYPE = "tt";
    public const string CLAIM_TOKEN_HASH = "th";
    public const string CLAIM_UNIQUE_ID = "uid";
    private static readonly JwtSecurityTokenHandler TokenHandler = new();

    public static string GenerateJwt(TokenType type, ResourceType resourceType,
                                     DateTimeOffset expires, string resourceId, string? uniqueId, string? scope,
                                     string? issuer,
                                     string? audience,
                                     string signingKey,
                                     string nameClaimType = ClaimTypes.NameIdentifier,
                                     string roleClaimType = ClaimTypes.Role,
                                     string? originScope = null) =>
        Generate(type, resourceType, expires, resourceId, uniqueId, scope, issuer, audience, signingKey, nameClaimType, roleClaimType, originScope).Token;

    public static (string Token, JwtSecurityToken Jwt, int ExpiresIn, long ExpiresAt, string Signature)
        Generate(TokenType type, ResourceType resourceType,
                 DateTimeOffset expires, string resourceId, string? uniqueId, string? scope,
                 string? issuer,
                 string? audience,
                 string signingKey,
                 string nameClaimType = ClaimTypes.NameIdentifier,
                 string roleClaimType = ClaimTypes.Role,
                 string? originScope = null)
    {
        var claims = new List<Claim>
                     {
                         new(JwtRegisteredClaimNames.Jti, GenerateCode(signingKey)),
                         new(CLAIM_TYPES_TOKEN_TYPE, ((int)type).ToString()),
                         new(CLAIM_TYPES_RESOURCE_TYPE, ((int)resourceType).ToString()),
                         new(nameClaimType, resourceId)
                     };

        if (!string.IsNullOrWhiteSpace(uniqueId)) claims.Add(new Claim(CLAIM_UNIQUE_ID, uniqueId));
        if (!string.IsNullOrWhiteSpace(scope)) claims.Add(new Claim(roleClaimType, scope));
        if (!string.IsNullOrWhiteSpace(originScope)) claims.Add(new Claim("origin-scope", originScope));

        var issuedAt = DateTimeOffset.UtcNow;
        var expiresIn = (int)expires.Subtract(issuedAt).TotalSeconds + 1;

        var tokenDescriptor = new SecurityTokenDescriptor
                              {
                                  Issuer = issuer,
                                  Audience = audience,
                                  Subject = new ClaimsIdentity(claims),
                                  IssuedAt = issuedAt.DateTime,
                                  NotBefore = issuedAt.DateTime,
                                  Expires = expires.DateTime,
                                  SigningCredentials = string.IsNullOrWhiteSpace(signingKey)
                                                           ? null
                                                           : new SigningCredentials(GetSymmetricSecurityKey(signingKey),
                                                                                    SecurityAlgorithms.HmacSha256)
                              };

        var jwtHandler = new JwtSecurityTokenHandler { SetDefaultTimesOnTokenCreation = false };

        var jwt = jwtHandler.CreateJwtSecurityToken(tokenDescriptor);

        var token = jwtHandler.WriteToken(jwt);

        return (token, jwt, expiresIn, expires.ToUnixTimeSeconds(), jwt.RawSignature);
    }

    public static string GenerateCode(string signingKey)
    {
        var hashids = new Hashids(signingKey);

        return hashids.EncodeLong(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    public static ClaimsPrincipal ValidateJwt(string token, TokenValidationParameters parameters)
    {
        var principal = TokenHandler.ValidateToken(token, parameters, out _);

        return principal;
    }

    public static JwtSecurityToken GetJwtInfo(string token)
    {
        var securityToken = TokenHandler.ReadJwtToken(token);

        return securityToken;
    }

    public static string? GetJwtSignature(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var jwt = token.Split(".");

        if (jwt.Length != 3)
            return null;

        return jwt[2];
    }

    public static SecurityKey GetSymmetricSecurityKey(string privateKey)
    {
        var symmetricKey = Encoding.UTF8.GetBytes(privateKey);

        return new SymmetricSecurityKey(symmetricKey);
    }

    public static bool ScopeCheck(string? entity, string? request)
    {
        var entityScope = entity?.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        var requestScope = request?.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

        return !requestScope.Except(entityScope).Any();
    }
}

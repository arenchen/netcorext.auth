using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Netcorext.Auth.Extensions;

public static class AuthOptionsExtension
{
    public static TokenValidationParameters GetTokenValidationParameters(this AuthOptions options)
    {
        return new TokenValidationParameters
               {
                   ValidIssuer = options.Issuer,
                   ValidateIssuer = options.ValidateIssuer,
                   ValidAudience = options.Audience,
                   ValidateAudience = options.ValidateAudience,
                   ValidateLifetime = options.ValidateLifetime,
                   ValidateIssuerSigningKey = options.ValidateIssuerSigningKey,
                   NameClaimType = options.NameClaimType,
                   RoleClaimType = options.RoleClaimType,
                   ClockSkew = TimeSpan.FromSeconds(options.ClockSkewSeconds),
                   IssuerSigningKey = string.IsNullOrWhiteSpace(options.SigningKey) ? null : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                   RequireExpirationTime = false
               };
    }
}
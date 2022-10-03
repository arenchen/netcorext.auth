using Netcorext.Auth.Authorization.Models;
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authorization.Services.User.Commands;

public class ExternalSignIn : IRequest<Result<TokenResult>>
{
    public string Username { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string UniqueId { get; set; } = null!;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Otp { get; set; }
    public int? TokenExpireSeconds { get; set; }
    public int? RefreshTokenExpireSeconds { get; set; }
    public int? CodeExpireSeconds { get; set; }
    public UserRole[]? Roles { get; set; }

    public class UserRole
    {
        public long RoleId { get; set; }
        public DateTimeOffset? ExpireDate { get; set; }
    }
}
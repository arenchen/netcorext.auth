using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User;

public class CreateUser : IRequest<Result<long?>>
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool RequiredChangePassword { get; set; }
    public int? TokenExpireSeconds { get; set; }
    public int? RefreshTokenExpireSeconds { get; set; }
    public int? CodeExpireSeconds { get; set; }
    public UserRole[]? Roles { get; set; }
    public UserExtendData[]? ExtendData { get; set; }
    public UserExternalLogin[]? ExternalLogins { get; set; }

    public class UserRole
    {
        public long RoleId { get; set; }
        public bool IsMain { get; set; }
        public DateTimeOffset? ExpireDate { get; set; }
        public long? ExpiredReverseRoleId { get; set; }
    }

    public class UserExtendData
    {
        public string Key { get; set; } = null!;
        public string? Value { get; set; }
    }

    public class UserExternalLogin
    {
        public string Provider { get; set; } = null!;
        public string UniqueId { get; set; } = null!;
    }
}
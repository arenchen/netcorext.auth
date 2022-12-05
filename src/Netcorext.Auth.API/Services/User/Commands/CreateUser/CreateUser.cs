using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Commands;

public class CreateUser : IRequest<Result<long?>>
{
    public long? CustomId { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool RequiredChangePassword { get; set; }
    public bool AllowedRefreshToken { get; set; }
    public int? TokenExpireSeconds { get; set; }
    public int? RefreshTokenExpireSeconds { get; set; }
    public int? CodeExpireSeconds { get; set; }
    public UserRole[]? Roles { get; set; }
    public UserExtendData[]? ExtendData { get; set; }
    public UserExternalLogin[]? ExternalLogins { get; set; }
    public UserPermissionCondition[]? PermissionConditions { get; set; }

    public class UserRole
    {
        public long RoleId { get; set; }
        public DateTimeOffset? ExpireDate { get; set; }
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

    public class UserPermissionCondition
    {
        public long PermissionId { get; set; }
        public int Priority { get; set; }
        public string? Group { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
        public bool Allowed { get; set; }
    }
}
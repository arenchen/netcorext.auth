using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Commands;

public class UpdateUser : IRequest<Result>
{
    public long Id { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? Password { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? TwoFactorEnabled { get; set; }
    public bool? RequiredChangePassword { get; set; }
    public bool? AllowedRefreshToken { get; set; }
    public int? TokenExpireSeconds { get; set; }
    public int? RefreshTokenExpireSeconds { get; set; }
    public int? CodeExpireSeconds { get; set; }
    public bool? Disabled { get; set; }
    public UserRole[]? Roles { get; set; }
    public UserExtendData[]? ExtendData { get; set; }
    public UserExternalLogin[]? ExternalLogins { get; set; }
    public UserPermissionCondition[]? PermissionConditions { get; set; }

    public class UserRole
    {
        public CRUD Crud { get; set; }
        public long RoleId { get; set; }
        public DateTimeOffset? ExpireDate { get; set; }
    }

    public class UserExtendData
    {
        public CRUD Crud { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }

    public class UserExternalLogin
    {
        public CRUD Crud { get; set; }
        public string Provider { get; set; } = null!;
        public string UniqueId { get; set; } = null!;
    }

    public class UserPermissionCondition
    {
        public CRUD Crud { get; set; }
        public long? Id { get; set; }
        public long PermissionId { get; set; }
        public string? Group { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
        public DateTimeOffset? ExpireDate { get; set; }
    }
}
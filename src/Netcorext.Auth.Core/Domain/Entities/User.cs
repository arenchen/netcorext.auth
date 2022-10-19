using Netcorext.EntityFramework.UserIdentityPattern.Entities;

namespace Netcorext.Auth.Domain.Entities;

public class User : Entity
{
    public string Username { get; set; } = null!;
    public string NormalizedUsername { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public string? Otp { get; set; }
    public bool OtpBound { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool RequiredChangePassword { get; set; }
    public bool AllowedRefreshToken { get; set; }
    public int? TokenExpireSeconds { get; set; }
    public int? RefreshTokenExpireSeconds { get; set; }
    public int? CodeExpireSeconds { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTimeOffset? LastSignInDate { get; set; }
    public string? LastSignInIp { get; set; }
    public bool Disabled { get; set; }

    public virtual ICollection<UserRole> Roles { get; set; } = new HashSet<UserRole>();
    public virtual ICollection<UserExtendData> ExtendData { get; set; } = new HashSet<UserExtendData>();
    public virtual ICollection<UserExternalLogin> ExternalLogins { get; set; } = new HashSet<UserExternalLogin>();
    public virtual ICollection<UserPermissionCondition> PermissionConditions { get; set; } = new HashSet<UserPermissionCondition>();
}
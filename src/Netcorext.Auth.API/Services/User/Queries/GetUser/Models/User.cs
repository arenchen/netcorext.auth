namespace Netcorext.Auth.API.Services.User.Queries.Models;

public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Email { get; set; }
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
    public DateTimeOffset? LastSignInDate { get; set; }
    public string? LastSignInIp { get; set; }
    public bool Disabled { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public long CreatorId { get; set; }
    public DateTimeOffset ModificationDate { get; set; }
    public long ModifierId { get; set; }
    public IEnumerable<UserRole>? Roles { get; set; }
    public IEnumerable<UserExtendData>? ExtendData { get; set; }
    public IEnumerable<UserExternalLogin>? ExternalLogins { get; set; }
    public IEnumerable<UserPermissionCondition>? PermissionConditions { get; set; }
}
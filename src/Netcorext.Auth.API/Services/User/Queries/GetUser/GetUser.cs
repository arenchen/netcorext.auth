using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUser : IRequest<Result<IEnumerable<Models.User>>>
{
    public long[]? Ids { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public bool? EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? PhoneNumberConfirmed { get; set; }
    public bool? Verified { get; set; }
    public bool? Disabled { get; set; }
    public UserRole? Role { get; set; }
    public UserExtendData[]? ExtendData { get; set; }
    public UserExternalLogin? ExternalLogin { get; set; }
    public string[]? LastSignInIps { get; set; }
    public Paging Paging { get; set; } = new();

    public class UserRole
    {
        public long? RoleId { get; set; }
        public string? Name { get; set; }
        public DateTimeOffset? ExpireDate { get; set; }
    }

    public class UserExtendData
    {
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }

    public class UserExternalLogin
    {
        public string? Provider { get; set; }
        public string? UniqueId { get; set; }
    }
}

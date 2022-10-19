using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Client.Commands;

public class UpdateClient : IRequest<Result>
{
    public long Id { get; set; }
    public string? Secret { get; set; }
    public string? Name { get; set; }
    public string? CallbackUrl { get; set; }
    public bool? AllowedRefreshToken { get; set; }
    public int? TokenExpireSeconds { get; set; }
    public int? RefreshTokenExpireSeconds { get; set; }
    public int? CodeExpireSeconds { get; set; }
    public bool? Disabled { get; set; }
    public ClientRole[]? Roles { get; set; }
    public ClientExtendData[]? ExtendData { get; set; }

    public class ClientRole
    {
        public CRUD Crud { get; set; }
        public long RoleId { get; set; }
        public DateTimeOffset? ExpireDate { get; set; }
    }

    public class ClientExtendData
    {
        public CRUD Crud { get; set; }
        public string Key { get; set; } = null!;
        public string? Value { get; set; }
    }
}
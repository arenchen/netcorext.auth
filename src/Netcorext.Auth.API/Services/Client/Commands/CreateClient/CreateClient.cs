using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Client.Commands;

public class CreateClient : IRequest<Result<long?>>
{
    public string Secret { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? CallbackUrl { get; set; }
    public bool AllowedRefreshToken { get; set; }
    public int? TokenExpireSeconds { get; set; }
    public int? RefreshTokenExpireSeconds { get; set; }
    public int? CodeExpireSeconds { get; set; }
    public bool Disabled { get; set; }
    public ClientRole[]? Roles { get; set; }
    public ClientExtendData[]? ExtendData { get; set; }

    public class ClientRole
    {
        public long RoleId { get; set; }
        public DateTimeOffset? ExpireDate { get; set; }
    }

    public class ClientExtendData
    {
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}
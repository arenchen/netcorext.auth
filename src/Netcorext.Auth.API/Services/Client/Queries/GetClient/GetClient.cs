using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Client.Queries;

public class GetClient : IRequest<Result<IEnumerable<Models.Client>>>
{
    public long? Id { get; set; }
    public string? Name { get; set; }
    public string? CallbackUrl { get; set; }
    public bool? Disabled { get; set; }
    public ClientRole? Role { get; set; }
    public ClientExtendData[]? ExtendData { get; set; }
    public Paging Paging { get; set; } = null!;

    public class ClientRole
    {
        public long? RoleId { get; set; }
        public string? Name { get; set; }
        public DateTimeOffset? ExpireDate { get; set; }
    }

    public class ClientExtendData
    {
        public string Key { get; set; } = null!;
        public string? Value { get; set; }
    }
}
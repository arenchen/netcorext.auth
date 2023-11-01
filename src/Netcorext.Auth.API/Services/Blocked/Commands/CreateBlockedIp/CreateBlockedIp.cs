using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Blocked.Commands;

public class CreateBlockedIp : IRequest<Result<IEnumerable<long>>>
{
    public BlockedIp[] BlockedIps { get; set; } = null!;

    public class BlockedIp
    {
        public string Cidr { get; set; } = null!;
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Asn { get; set; }
        public string? Description { get; set; }
    }
}

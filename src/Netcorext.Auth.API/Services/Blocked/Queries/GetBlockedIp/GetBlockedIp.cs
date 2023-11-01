using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Blocked.Queries;

public class GetBlockedIp : IRequest<Result<IEnumerable<Models.BlockedIp>>>
{
    public long? Id { get; set; }
    public string? Cidr { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Asn { get; set; }
    public Paging Paging { get; set; } = null!;
}

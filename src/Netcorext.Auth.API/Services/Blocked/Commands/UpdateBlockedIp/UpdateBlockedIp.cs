using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Blocked.Commands;

public class UpdateBlockedIp : IRequest<Result>
{
    public long Id { get; set; }
    public string? Cidr { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Asn { get; set; }
    public string? Description { get; set; }
}

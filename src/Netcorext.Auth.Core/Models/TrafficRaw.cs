using System.Security.Claims;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Netcorext.Auth.Models;

public class TrafficRaw
{
    public DateTimeOffset Timestamp { get; set; }
    public string Protocol { get; set; } = null!;
    public string Scheme { get; set; } = null!;
    public string HttpMethod { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string Host { get; set; } = null!;
    public string Path { get; set; } = null!;
    public string? QueryString { get; set; }
    public HeaderDictionary? Headers { get; set; }
    public HeaderDictionary? ResponseHeaders { get; set; }
    public ClaimsPrincipal? User { get; set; }
    public string? Status { get; set; }
    public string? Ip { get; set; }
    public string? TraceIdentifier { get; set; }
}

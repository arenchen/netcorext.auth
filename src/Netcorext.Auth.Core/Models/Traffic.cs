namespace Netcorext.Auth.Models;

public class Traffic
{
    public DateTimeOffset TrafficDate { get; set; }
    public string Protocol { get; set; } = null!;
    public string Scheme { get; set; } = null!;
    public string HttpMethod { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string Host { get; set; } = null!;
    public string Path { get; set; } = null!;
    public string? QueryString { get; set; }
    public string? Headers { get; set; }
    public string? ResponseHeaders { get; set; }
    public string Status { get; set; } = null!;
    public TimeSpan Elapsed { get; set; }
    public string? DeviceId { get; set; }
    public string? Ip { get; set; }
    public string? TraceIdentifier { get; set; }
    public string? XRequestId { get; set; }
    public Dictionary<string, string?>? UserAgent { get; set; }
    public Dictionary<string, string?>? User { get; set; }
}

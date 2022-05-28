using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Netcorext.Auth.Extensions;

public static class HttpContextExtension
{
    private static readonly string[] DefaultIpHeaderName = { "X-Origin-Forwarded-For", "X-Forwarded-For", "X-Real-Ip" };
    private static readonly Regex RegexIp = new Regex(@"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}[^,]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string? GetIp(this HttpContext context, params string[]? headerNames)
    {
        var ip = context.Connection
                        .RemoteIpAddress?.MapToIPv4()
                        .ToString()
                        .ToLower();

        if (headerNames == null || !headerNames.Any()) headerNames = DefaultIpHeaderName;

        var verifyIp = string.Empty;

        foreach (var name in headerNames)
        {
            if (!context.Request.Headers.TryGetValue(name, out var headerValue) || !RegexIp.IsMatch(headerValue)) continue;

            verifyIp = RegexIp.Match(headerValue).Groups[1].Value;

            break;
        }

        if (IPAddress.TryParse(verifyIp, out var addr)) ip = addr.MapToIPv4().ToString();

        return ip?.ToLower();
    }
}
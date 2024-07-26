using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Netcorext.Auth.Extensions;

public static class HttpContextExtension
{
    private static readonly string[] DefaultIpHeaderName = { "X-Origin-Forwarded-For", "X-Forwarded-For", "X-Real-Ip" };
    private static readonly Regex RegexIp = new(@"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}[^,]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex RegexLastPath = new(@"/(\w+)$", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private const string HEADER_REQUEST_ID = "X-Request-Id";
    private const string HEADER_DEVICE_ID = "X-Device-Id";

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


    public static string GetRequestHeaders(this HttpContext context)
    {
        var headers = context.Request.Headers;
        var headersString = new StringBuilder();
        foreach (var header in headers)
        {
            headersString.Append($"{header.Key}: {header.Value}\n");
        }
        return headersString.ToString();
    }

    public static string GetResponseHeaders(this HttpContext context)
    {
        var headers = context.Response.Headers;
        var headersString = new StringBuilder();
        foreach (var header in headers)
        {
            headersString.Append($"{header.Key}: {header.Value}\n");
        }
        return headersString.ToString();
    }

    public static Dictionary<string, string?>? GetUser(this HttpContext context)
    {
        if (!context.User.Claims.Any())
            return default;

        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var claim in context.User.Claims)
        {
            switch (claim.Type)
            {
                case ClaimTypes.Name:
                    result.TryAdd(JwtRegisteredClaimNames.Name, claim.Value);

                    break;
                case ClaimTypes.NameIdentifier:
                    result.TryAdd(JwtRegisteredClaimNames.NameId, claim.Value);

                    break;
                case ClaimTypes.Role:
                    result.TryAdd(GetLastPath(ClaimTypes.Role), claim.Value);

                    break;
                default:
                    result.TryAdd(claim.Type, claim.Value);

                    break;
            }
        }

        return result;
    }

    public static Dictionary<string, string?>? GetUserAgent(this HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Request.Headers.UserAgent))
            return default;

        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var clientInfo = UAParser.Parser.GetDefault().Parse(context.Request.Headers.UserAgent);

        var device = clientInfo.Device.ToString();
        var os = clientInfo.OS.ToString();
        var ua = clientInfo.UA.ToString();
        var deviceType = ua.Contains("Mobile", StringComparison.CurrentCultureIgnoreCase) ? "Mobile" : os.Contains("Android", StringComparison.CurrentCultureIgnoreCase) ? "Tablet" : "Desktop";

        result.TryAdd("device", device);
        result.TryAdd("deviceType", deviceType);
        result.TryAdd("os", os);
        result.TryAdd("ua", ua);

        return result;
    }

    public static string GetRequestId(this HttpContext context, params string[] headerNames)
    {
        var requestId = context.TraceIdentifier;

        var headers = headerNames.Length == 0 ? new[] { HEADER_REQUEST_ID } : headerNames;

        foreach (var name in headers)
        {
            if (!context.Request.Headers.TryGetValue(name, out var hRequestId) || string.IsNullOrWhiteSpace(hRequestId))
                continue;

            requestId = hRequestId;

            break;
        }

        return requestId ?? string.Empty;
    }

    public static string? GetDeviceId(this HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HEADER_DEVICE_ID, out var deviceId)
         && !string.IsNullOrWhiteSpace(deviceId))
            return deviceId.ToString().ToLower();

        return null;
    }

    public static string GetLastPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;

        var m = RegexLastPath.Match(path);

        return !m.Success ? path : m.Groups[1].Value;
    }
}

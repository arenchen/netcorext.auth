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

    private static readonly Regex RegIPad = new Regex(@"\(iPad.*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RegIPhone = new Regex(@"\(iPhone.*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RegAndroid = new Regex(@"\(Android.*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RegMobile = new Regex(@"\(Mobile.*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RegWindows = new Regex(@"\(Windows.*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RegLinux = new Regex(@"\(Linux.*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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


    public static string GetHeadersString(this IHeaderDictionary headers)
    {
        var headersString = new StringBuilder();
        foreach (var header in headers)
        {
            headersString.Append($"{header.Key}: {header.Value}\n");
        }
        return headersString.ToString();
    }

    public static Dictionary<string, string?>? GetUser(this ClaimsPrincipal user)
    {
        if (!user.Claims.Any())
            return default;

        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var claim in user.Claims)
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

    public static Dictionary<string, string?>? GetUserAgent(this IHeaderDictionary headers)
    {
        if (string.IsNullOrWhiteSpace(headers.UserAgent))
            return default;

        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var isIpad = RegIPad.IsMatch(headers.UserAgent);
        var isIphone = RegIPhone.IsMatch(headers.UserAgent);
        var isAndroid = RegAndroid.IsMatch(headers.UserAgent);
        var isWindows = RegWindows.IsMatch(headers.UserAgent);
        var isLinux = RegLinux.IsMatch(headers.UserAgent);
        var isMobile = RegMobile.IsMatch(headers.UserAgent);

        if (isIpad)
        {
            result.TryAdd("device", "iPad");
            result.TryAdd("deviceType", "Tablet");
        }
        else if (isIphone)
        {
            result.TryAdd("device", "iPhone");
            result.TryAdd("deviceType", "Mobile");
        }
        else if (isAndroid && isMobile)
        {
            result.TryAdd("device", "Android");
            result.TryAdd("deviceType", "Mobile");
        }
        else if (isAndroid)
        {
            result.TryAdd("device", "Android");
            result.TryAdd("deviceType", "Tablet");
        }
        else if (isWindows && isMobile)
        {
            result.TryAdd("device", "Windows Phone");
            result.TryAdd("deviceType", "Mobile");
        }
        else if (isWindows)
        {
            result.TryAdd("device", "Windows");
            result.TryAdd("deviceType", "Desktop");
        }
        else if (isLinux)
        {
            result.TryAdd("device", "Linux");
            result.TryAdd("deviceType", "Desktop");
        }
        else
        {
            result.TryAdd("device", "Other");
            result.TryAdd("deviceType", "Desktop");
        }

        return result;
    }

    public static string GetRequestId(this IHeaderDictionary headers, params string[] headerNames)
    {
        var requestId = string.Empty;

        var internalheaderNames = headerNames.Length == 0 ? new[] { HEADER_REQUEST_ID } : headerNames;

        foreach (var name in internalheaderNames)
        {
            if (!headers.TryGetValue(name, out var hRequestId) || string.IsNullOrWhiteSpace(hRequestId))
                continue;

            requestId = hRequestId;

            break;
        }

        return requestId ?? string.Empty;
    }

    public static string? GetDeviceId(this IHeaderDictionary headers)
    {
        if (headers.TryGetValue(HEADER_DEVICE_ID, out var deviceId)
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

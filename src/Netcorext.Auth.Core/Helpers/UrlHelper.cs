using Microsoft.AspNetCore.Server.HttpSys;

namespace Netcorext.Auth.Helpers;

public static class UrlHelper
{
    public static string GetHostAndPort(string url)
    {
        var urlPrefix = UrlPrefix.Create(url);

        return $"{urlPrefix.Host}:{urlPrefix.Port}";
    }
}
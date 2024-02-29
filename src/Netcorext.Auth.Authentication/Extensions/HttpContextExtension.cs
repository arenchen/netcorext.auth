using System.Text;
using Grpc.Core;
using Netcorext.Contracts;

namespace Netcorext.Auth.Authentication.Extensions;

public static class HttpContextExtension
{
    private static readonly Dictionary<string, string> ErrorMessages = new()
                                                                       {
                                                                           { "401000", "{ \"code\": \"401000\", \"message\": \"Unauthorized.\"}" },
                                                                           { "401001", "{ \"code\": \"401001\", \"message\": \"Unauthorized and cannot refresh token.\"}" },
                                                                           { "403000", "{ \"code\": \"403000\", \"message\": \"Forbidden.\"}" },
                                                                           { "403002", "{ \"code\": \"403002\", \"message\": \"The specified account is disabled.\"}" },
                                                                           { "403007", "{ \"code\": \"403007\", \"message\": \"Permission has changed, please re-login\"}" },
                                                                           { "403008", "{ \"code\": \"403008\", \"message\": \"This specified ip is blocked.\"}" }
                                                                       };

    public static bool IsGrpc(this HttpContext context)
    {
        return context.Request.Protocol == "HTTP/2" && context.Request.Headers.ContentType == "application/grpc";
    }

    public static async Task UnauthorizedAsync(this HttpContext context, bool useNativeStatus = true, string? code = "401000")
    {
        code ??= Result.Unauthorized;

        var message = ErrorMessages[code];

        if (context.IsGrpc())
        {
            context.Response.StatusCode = useNativeStatus ? 401 : 200;
            context.Response.ContentType = "application/grpc";
            context.Response.Headers.Add("Trailers-Only", "true");
            context.Response.AppendTrailer("grpc-status", StatusCode.Unauthenticated.ToString("D"));
            context.Response.AppendTrailer("grpc-message", message);

            return;
        }

        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(message));
    }

    public static async Task ForbiddenAsync(this HttpContext context, bool useNativeStatus = true, string? code = "403000")
    {
        code ??= Result.Forbidden;

        var message = ErrorMessages[code];

        if (context.IsGrpc())
        {
            context.Response.StatusCode = useNativeStatus ? 403 : 200;
            context.Response.ContentType = "application/grpc";
            context.Response.Headers.Add("Trailers-Only", "true");
            context.Response.AppendTrailer("grpc-status", StatusCode.PermissionDenied.ToString("D"));
            context.Response.AppendTrailer("grpc-message", message);

            return;
        }

        context.Response.StatusCode = 403;
        context.Response.ContentType = "application/json";
        await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(message));
    }

    public static async Task ServiceUnavailableAsync(this HttpContext context, bool useNativeStatus = true, string? code = "503000", string? message = "Service Unavailable")
    {
        code ??= Result.ServiceUnavailable;

        var msg = message ?? ErrorMessages[code];

        if (context.IsGrpc())
        {
            context.Response.StatusCode = useNativeStatus ? 503 : 200;
            context.Response.ContentType = "application/grpc";
            context.Response.Headers.Add("Trailers-Only", "true");
            context.Response.AppendTrailer("grpc-status", StatusCode.Unavailable.ToString("D"));
            context.Response.AppendTrailer("grpc-message", msg);

            return;
        }

        context.Response.StatusCode = 503;
        context.Response.ContentType = "application/json";
        await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(msg));
    }

    public static string GetPath(this HttpRequest request)
    {
        return request.Headers.TryGetValue("X-Forwarded-Uri", out var xpath) ? xpath : request.Path;
    }

    public static string GetMethod(this HttpRequest request)
    {
        return request.Headers.TryGetValue("X-Forwarded-Method", out var xmethod) ? xmethod : request.Method;
    }
}

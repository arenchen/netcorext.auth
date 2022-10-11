using System.Text;
using Grpc.Core;

namespace Netcorext.Auth.Authentication.Extensions;

public static class HttpContextExtension
{
    private const string ERROR_MESSAGE_UNAUTHORIZED = "{ \"code\": \"401000\", \"message\": \"Unauthorized.\"}";
    private const string ERROR_MESSAGE_FORBIDDEN = "{ \"code\": \"403000\", \"message\": \"Forbidden.\"}";

    public static bool IsGrpc(this HttpContext context)
    {
        return context.Request.Protocol == "HTTP/2" && context.Request.Headers.ContentType == "application/grpc";
    }

    public static async Task UnauthorizedAsync(this HttpContext context, bool useNativeStatus = true)
    {
        if (context.IsGrpc())
        {
            context.Response.StatusCode = useNativeStatus ? 401 : 200;
            context.Response.ContentType = "application/grpc";
            context.Response.Headers.Add("Trailers-Only", "true");
            context.Response.AppendTrailer("grpc-status", StatusCode.Unauthenticated.ToString("D"));
            context.Response.AppendTrailer("grpc-message", ERROR_MESSAGE_UNAUTHORIZED);

            return;
        }

        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(ERROR_MESSAGE_UNAUTHORIZED));
    }

    public static async Task ForbiddenAsync(this HttpContext context, bool useNativeStatus = true)
    {
        if (context.IsGrpc())
        {
            context.Response.StatusCode = useNativeStatus ? 403 : 200;
            context.Response.ContentType = "application/grpc";
            context.Response.Headers.Add("Trailers-Only", "true");
            context.Response.AppendTrailer("grpc-status", StatusCode.PermissionDenied.ToString("D"));
            context.Response.AppendTrailer("grpc-message", ERROR_MESSAGE_FORBIDDEN);

            return;
        }

        context.Response.StatusCode = 403;
        context.Response.ContentType = "application/json";
        await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(ERROR_MESSAGE_FORBIDDEN));
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
using System.Security.Claims;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Extensions;
using Netcorext.Auth.Authentication.Services.Permission;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Auth.Extensions;
using Netcorext.Contracts;
using Netcorext.Extensions.Commons;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Middlewares;

internal class PermissionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ConfigSettings _config;
    private IDispatcher _dispatcher;

    public PermissionMiddleware(RequestDelegate next, IMemoryCache cache, IOptions<ConfigSettings> config, IDispatcher dispatcher)
    {
        _next = next;
        _cache = cache;
        _config = config.Value;
        _dispatcher = dispatcher;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_config.AppSettings.InternalHost.Equals(context.Request.Host.Host, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);

            return;
        }

        _dispatcher = context.RequestServices.GetRequiredService<IDispatcher>();

        var permissionEndpoints = _cache.Get<Dictionary<long, Services.Route.Models.RouteGroup>>(ConfigSettings.CACHE_ROUTE) ?? new Dictionary<long, Services.Route.Models.RouteGroup>();
        var allowAnonymous = false;
        var functionId = string.Empty;
        var role = context.User.Claims.FirstOrDefault(t => t.Type == ClaimTypes.Role)?.Value;
        var path = context.Request.GetPath();
        var method = context.Request.GetMethod();

        var endpoints = permissionEndpoints.Values.SelectMany(t => t.Routes);

        foreach (var endpoint in endpoints)
        {
            var template = TemplateParser.Parse(endpoint.Template);

            var matcher = new TemplateMatcher(template, new RouteValueDictionary(endpoint.RouteValues));

            if (!matcher.TryMatch(path, new RouteValueDictionary(context.Request.RouteValues))) continue;

            allowAnonymous = endpoint.AllowAnonymous;
            functionId = endpoint.FunctionId;

            break;
        }

        if (allowAnonymous || string.IsNullOrWhiteSpace(functionId) || await IsValidAsync(role, functionId, method, context.Request.RouteValues))
        {
            await _next(context);

            return;
        }

        if (!allowAnonymous)
        {
            var headerValue = context.Request.Headers["Authorization"];

            if (headerValue.IsEmpty())
            {
                await context.UnauthorizedAsync(_config.AppSettings.UseNativeStatus);

                return;
            }
        }

        await context.ForbiddenAsync(_config.AppSettings.UseNativeStatus);
    }

    private async Task<bool> IsValidAsync(string? role, string functionId, string httpMethod, RouteValueDictionary routeValues)
    {
        if (string.IsNullOrWhiteSpace(role)) return false;

        var roleIds = role.Split(" ", StringSplitOptions.RemoveEmptyEntries)
                          .Where(t => !t.IsEmpty() && long.TryParse(t, out var _))
                          .Select(long.Parse)
                          .ToArray();

        var result = await _dispatcher.SendAsync(new ValidatePermission
                                                 {
                                                     RoleId = roleIds,
                                                     FunctionId = functionId,
                                                     PermissionType = httpMethod.ToPermissionType(),
                                                     ExtendData = routeValues.Select(t => new ValidatePermission.PermissionExtendData
                                                                                          {
                                                                                              Key = t.Key,
                                                                                              Value = t.Value?.ToString()!
                                                                                          })
                                                                             .ToArray()
                                                 });

        return result == Result.Success;
    }
}
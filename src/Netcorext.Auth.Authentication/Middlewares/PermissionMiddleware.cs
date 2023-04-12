using System.Security.Claims;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Extensions;
using Netcorext.Auth.Authentication.Services.Permission.Queries;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Auth.Extensions;
using Netcorext.Auth.Helpers;
using Netcorext.Contracts;
using Netcorext.Extensions.Commons;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Middlewares;

internal class PermissionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionMiddleware> _logger;
    private readonly ConfigSettings _config;

    public PermissionMiddleware(RequestDelegate next, IMemoryCache cache, IOptions<ConfigSettings> config, ILogger<PermissionMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _config = config.Value;
    }

    public async Task InvokeAsync(HttpContext context, IDispatcher dispatcher)
    {
        if (_config.AppSettings.InternalHost?.Any(t => t.Equals(context.Request.Host.Host, StringComparison.CurrentCultureIgnoreCase)) ?? false)
        {
            await _next(context);

            return;
        }

        var claimName = context.User.Claims.FirstOrDefault(t => t.Type == ClaimTypes.Name)?.Value;

        if (long.TryParse(claimName, out var id) && (_config.AppSettings.Owner?.Any(t => t == id) ?? false))
        {
            await _next(context);

            return;
        }

        var permissionEndpoints = _cache.Get<Dictionary<long, Services.Route.Queries.Models.RouteGroup>>(ConfigSettings.CACHE_ROUTE) ?? new Dictionary<long, Services.Route.Queries.Models.RouteGroup>();
        var allowAnonymous = false;
        var functionId = string.Empty;

        var rt = context.User.Claims.FirstOrDefault(t => t.Type == TokenHelper.CLAIM_TYPES_RESOURCE_TYPE)?.Value;
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

        if (allowAnonymous || string.IsNullOrWhiteSpace(functionId) || await IsValidAsync(dispatcher, _config.AppSettings.ValidationPassUserId && rt == "1" ? id : null, role, functionId, method, context.Request.RouteValues))
        {
            await _next(context);

            return;
        }

        if (!allowAnonymous)
        {
            var headerValue = context.Request.Headers["Authorization"];

            if (headerValue.IsEmpty())
            {
                _logger.LogWarning("Unauthorized, header 'Authorization' value is empty");

                await context.UnauthorizedAsync(_config.AppSettings.UseNativeStatus);

                return;
            }
        }

        _logger.LogWarning("Forbidden");

        await context.ForbiddenAsync(_config.AppSettings.UseNativeStatus);
    }

    private async Task<bool> IsValidAsync(IDispatcher dispatcher, long? userId, string? role, string functionId, string httpMethod, RouteValueDictionary routeValues)
    {
        var roleIds = role?.Split(" ", StringSplitOptions.RemoveEmptyEntries)
                           .Where(t => !t.IsEmpty() && long.TryParse(t, out var _))
                           .Select(long.Parse)
                           .ToArray();

        if (userId.IsEmpty() && roleIds.IsEmpty()) return false;

        var result = await dispatcher.SendAsync(new ValidatePermission
                                                {
                                                    UserId = userId.IsEmpty() ? null : userId,
                                                    RoleId = roleIds,
                                                    FunctionId = functionId,
                                                    PermissionType = httpMethod.ToPermissionType(),
                                                    PermissionConditions = routeValues.Select(t => new ValidatePermission.PermissionCondition
                                                                                                   {
                                                                                                       Key = t.Key,
                                                                                                       Value = t.Value?.ToString()!
                                                                                                   })
                                                                                      .ToArray()
                                                });

        return result == Result.Success;
    }
}
using Grpc.Core;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Netcorext.Auth.Attributes;
using Netcorext.Auth.Authentication.Services.Route.Commands;
using Netcorext.Auth.Enums;
using Netcorext.Auth.Protobufs;
using Netcorext.Contracts.Protobufs;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Route;

[AllowAnonymous]
[Permission("AUTH", PermissionType.Write)]
public class RouteServiceFacade : RouteService.RouteServiceBase
{
    private readonly IDispatcher _dispatcher;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RouteServiceFacade(IDispatcher dispatcher, IHttpContextAccessor httpContextAccessor)
    {
        _dispatcher = dispatcher;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<Result> RegisterRoute(RegisterRouteRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<RegisterRoute>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<Result>();
    }
}
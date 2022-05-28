using Grpc.Core;
using Mapster;
using Netcorext.Auth.Attributes;
using Netcorext.Auth.Enums;
using Netcorext.Auth.Protobufs;
using Netcorext.Contracts.Protobufs;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role;

[Permission("AUTH")]
public class RoleServiceFacade : RoleService.RoleServiceBase
{
    private readonly IDispatcher _dispatcher;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RoleServiceFacade(IDispatcher dispatcher, IHttpContextAccessor httpContextAccessor)
    {
        _dispatcher = dispatcher;
        _httpContextAccessor = httpContextAccessor;
    }
    
    [Permission("AUTH", PermissionType.Write)]
    public override async Task<CreateRoleRequest.Types.Result> CreateRole(CreateRoleRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<CreateRole>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<CreateRoleRequest.Types.Result>();
    }
    
    [Permission("AUTH", PermissionType.Delete)]
    public override async Task<Result> DeleteRole(DeleteRoleRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<DeleteRole>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<Result>();
    }

    [Permission("AUTH", PermissionType.Read)]
    public override async Task<Result> RoleExists(RoleExistsRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<ExistsRole>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<Result>();
    }

    [Permission("AUTH", PermissionType.Read)]
    public override async Task<GetRoleRequest.Types.Result> GetRole(GetRoleRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<GetRole>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<GetRoleRequest.Types.Result>();
    }

    [Permission("AUTH", PermissionType.Write)]
    public override async Task<Result> UpdateRole(UpdateRoleRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<UpdateRole>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<Result>();
    }
}
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Mapster;
using Netcorext.Auth.API.Services.Permission.Commands;
using Netcorext.Auth.API.Services.Permission.Queries;
using Netcorext.Auth.Protobufs;
using Netcorext.Contracts.Protobufs;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Permission;

public class PermissionServiceFacade : PermissionService.PermissionServiceBase
{
    private readonly IDispatcher _dispatcher;

    public PermissionServiceFacade(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public override async Task<CreatePermissionRequest.Types.Result> CreatePermission(CreatePermissionRequest request, ServerCallContext context)
    {
        var req = request.Adapt<CreatePermission>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<CreatePermissionRequest.Types.Result>();
    }

    public override async Task<Result> DeletePermission(DeletePermissionRequest request, ServerCallContext context)
    {
        var req = request.Adapt<DeletePermission>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<Result>();
    }

    public override async Task<GetPermissionRequest.Types.Result> GetPermission(GetPermissionRequest request, ServerCallContext context)
    {
        var req = request.Adapt<GetPermission>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<GetPermissionRequest.Types.Result>();
    }

    public override async Task<Result> UpdatePermission(UpdatePermissionRequest request, ServerCallContext context)
    {
        var req = request.Adapt<UpdatePermission>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<Result>();
    }

    public override async Task<GetRouteFunctionResult> GetRouteFunction(Empty request, ServerCallContext context)
    {
        var req = new GetRouteFunction();
        var rep = await _dispatcher.SendAsync(req);

        var result = new GetRouteFunctionResult
                     {
                         Code = rep.Code,
                         Message = rep.Message,
                         Content = { rep.Content }
                     };
        
        return result;
    }
}
using Grpc.Core;
using Mapster;
using Netcorext.Auth.API.Services.Blocked.Commands;
using Netcorext.Auth.API.Services.Blocked.Queries;
using Netcorext.Auth.Attributes;
using Netcorext.Auth.Enums;
using Netcorext.Auth.Protobufs;
using Netcorext.Contracts.Protobufs;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Blocked;

[Permission("AUTH")]
public class BlockedServiceFacade : BlockedService.BlockedServiceBase
{
    private readonly IDispatcher _dispatcher;

    public BlockedServiceFacade(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [Permission("AUTH", PermissionType.Write)]
    public override async Task<CreateBlockedIpRequest.Types.Result> CreateBlockedIp(CreateBlockedIpRequest request, ServerCallContext context)
    {
        var req = request.Adapt<CreateBlockedIp>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<CreateBlockedIpRequest.Types.Result>();
    }

    [Permission("AUTH", PermissionType.Delete)]
    public override async Task<Result> DeleteBlockedIp(DeleteBlockedIpRequest request, ServerCallContext context)
    {
        var req = request.Adapt<DeleteBlockedIp>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<Result>();
    }

    [Permission("AUTH", PermissionType.Read)]
    public override async Task<GetBlockedIpRequest.Types.Result> GetBlockedIp(GetBlockedIpRequest request, ServerCallContext context)
    {
        var req = request.Adapt<GetBlockedIp>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<GetBlockedIpRequest.Types.Result>();
    }

    [Permission("AUTH", PermissionType.Write)]
    public override async Task<Result> UpdateBlockedIp(UpdateBlockedIpRequest request, ServerCallContext context)
    {
        var req = request.Adapt<UpdateBlockedIp>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<Result>();
    }
}

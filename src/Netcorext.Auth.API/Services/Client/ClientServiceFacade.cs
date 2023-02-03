using Grpc.Core;
using Mapster;
using Netcorext.Auth.API.Services.Client.Commands;
using Netcorext.Auth.API.Services.Client.Queries;
using Netcorext.Auth.Attributes;
using Netcorext.Auth.Enums;
using Netcorext.Auth.Protobufs;
using Netcorext.Contracts.Protobufs;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Client;

[Permission("AUTH")]
public class ClientServiceFacade : ClientService.ClientServiceBase
{
    private readonly IDispatcher _dispatcher;

    public ClientServiceFacade(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [Permission("AUTH", PermissionType.Write)]
    public override async Task<CreateClientRequest.Types.Result> CreateClient(CreateClientRequest request, ServerCallContext context)
    {
        var req = request.Adapt<CreateClient>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<CreateClientRequest.Types.Result>();
    }

    [Permission("AUTH", PermissionType.Delete)]
    public override async Task<Result> DeleteClient(DeleteClientRequest request, ServerCallContext context)
    {
        var req = request.Adapt<DeleteClient>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<Result>();
    }

    [Permission("AUTH", PermissionType.Read)]
    public override async Task<GetClientRequest.Types.Result> GetClient(GetClientRequest request, ServerCallContext context)
    {
        var req = request.Adapt<GetClient>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<GetClientRequest.Types.Result>();
    }

    public override async Task<GetClientPermissionRequest.Types.Result> GetClientPermission(GetClientPermissionRequest request, ServerCallContext context)
    {
        var req = request.Adapt<GetClientPermission>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<GetClientPermissionRequest.Types.Result>();
    }

    [Permission("AUTH", PermissionType.Write)]
    public override async Task<Result> UpdateClient(UpdateClientRequest request, ServerCallContext context)
    {
        var req = request.Adapt<UpdateClient>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<Result>();
    }
}
using Grpc.Core;
using Mapster;
using Netcorext.Auth.API.Services.Maintenance.Commands;
using Netcorext.Auth.API.Services.Maintenance.Queries;
using Netcorext.Auth.Protobufs;
using Netcorext.Contracts.Protobufs;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Maintenance;

public class MaintenanceServiceFacade : MaintenanceService.MaintenanceServiceBase
{
    private readonly IDispatcher _dispatcher;

    public MaintenanceServiceFacade(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public override async Task<Result> CreateMaintain(CreateMaintainRequest request, ServerCallContext context)
    {
        var req = request.Adapt<CreateMaintain>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<Result>();
    }

    public override async Task<Result> UpdateMaintain(UpdateMaintainRequest request, ServerCallContext context)
    {
        var req = request.Adapt<UpdateMaintain>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<Result>();
    }

    public override async Task<Result> DeleteMaintain(DeleteMaintainRequest request, ServerCallContext context)
    {
        var req = request.Adapt<DeleteMaintain>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<Result>();
    }

    public override async Task<GetMaintainRequest.Types.Result> GetMaintain(GetMaintainRequest request, ServerCallContext context)
    {
        var req = request.Adapt<GetMaintain>();
        var rep = await _dispatcher.SendAsync(req);
        var result = rep.Adapt<GetMaintainRequest.Types.Result>();

        if (rep.Content == null)
            return result;

        foreach (var item in rep.Content)
        {
            result.Content.Add(item.Key, item.Value.Adapt<GetMaintainRequest.Types.Result.Types.MaintainItem>());
        }

        return result;
    }
}

using Grpc.Core;
using Mapster;
using Netcorext.Auth.Authentication.Services.Maintenance.Commands;
using Netcorext.Auth.Protobufs;
using Netcorext.Contracts.Protobufs;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Maintenance;

public class MaintenanceServiceFacade : MaintenanceService.MaintenanceServiceBase
{
    private readonly IDispatcher _dispatcher;

    public MaintenanceServiceFacade(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public override async Task<Result> Maintain(MaintainRequest request, ServerCallContext context)
    {
        var req = request.Adapt<Maintain>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<Result>();
    }
}
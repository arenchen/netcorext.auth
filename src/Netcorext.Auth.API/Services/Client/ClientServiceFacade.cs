using Grpc.Core;
using Mapster;
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
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ClientServiceFacade> _logger;

    public ClientServiceFacade(IDispatcher dispatcher, IHttpContextAccessor httpContextAccessor, ILogger<ClientServiceFacade> logger)
    {
        _dispatcher = dispatcher;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    [Permission("AUTH", PermissionType.Write)]
    public override async Task<CreateClientRequest.Types.Result> CreateClient(CreateClientRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<CreateClient>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<CreateClientRequest.Types.Result>();
    }

    [Permission("AUTH", PermissionType.Delete)]
    public override async Task<Result> DeleteClient(DeleteClientRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<DeleteClient>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<Result>();
    }

    [Permission("AUTH", PermissionType.Read)]
    public override async Task<GetClientRequest.Types.Result> GetClient(GetClientRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<GetClient>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<GetClientRequest.Types.Result>();
    }

    [Permission("AUTH", PermissionType.Write)]
    public override async Task<Result> UpdateClient(UpdateClientRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<UpdateClient>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<Result>();
    }
}
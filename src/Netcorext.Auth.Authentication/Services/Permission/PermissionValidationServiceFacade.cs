using Grpc.Core;
using Mapster;
using Netcorext.Auth.Authentication.Services.Permission.Queries;
using Netcorext.Auth.Protobufs;
using Netcorext.Contracts.Protobufs;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Permission;

public class PermissionValidationServiceFacade : PermissionValidationService.PermissionValidationServiceBase
{
    private readonly IDispatcher _dispatcher;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionValidationServiceFacade(IDispatcher dispatcher, IHttpContextAccessor httpContextAccessor)
    {
        _dispatcher = dispatcher;
        _httpContextAccessor = httpContextAccessor;
    }
    
    public override async Task<Result> ValidatePermission(ValidatePermissionRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<ValidatePermission>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<Result>();
    }
}
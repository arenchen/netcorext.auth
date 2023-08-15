using Grpc.Core;
using Mapster;
using Netcorext.Auth.API.Services.User.Commands;
using Netcorext.Auth.API.Services.User.Queries;
using Netcorext.Auth.Attributes;
using Netcorext.Auth.Enums;
using Netcorext.Auth.Protobufs;
using Netcorext.Contracts.Protobufs;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User;

[Permission("AUTH")]
public class UserServiceFacade : UserService.UserServiceBase
{
    private readonly IDispatcher _dispatcher;

    public UserServiceFacade(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [Permission("AUTH", PermissionType.Write)]
    public override async Task<CreateUserRequest.Types.Result> CreateUser(CreateUserRequest request, ServerCallContext context)
    {
        var req = request.Adapt<CreateUser>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<CreateUserRequest.Types.Result>();
    }

    [Permission("AUTH", PermissionType.Delete)]
    public override async Task<Result> DeleteUser(DeleteUserRequest request, ServerCallContext context)
    {
        var req = request.Adapt<DeleteUser>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<Result>();
    }

    [Permission("AUTH", PermissionType.Read)]
    public override async Task<Result> ExistsUser(ExistsUserRequest request, ServerCallContext context)
    {
        var req = request.Adapt<ExistsUser>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<Result>();
    }

    [Permission("AUTH", PermissionType.Read)]
    public override async Task<GetUserRequest.Types.Result> GetUser(GetUserRequest request, ServerCallContext context)
    {
        var req = request.Adapt<GetUser>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<GetUserRequest.Types.Result>();
    }

    public override async Task<GetUserIdentityRequest.Types.Result> GetUserIdentity(GetUserIdentityRequest request, ServerCallContext context)
    {
        var req = request.Adapt<GetUserIdentity>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<GetUserIdentityRequest.Types.Result>();
    }

    [Permission("AUTH", PermissionType.Read)]
    public override async Task<GetUserFunctionRequest.Types.Result> GetUserFunction(GetUserFunctionRequest request, ServerCallContext context)
    {
        var req = request.Adapt<GetUserFunction>();
        var rep = await _dispatcher.SendAsync(req);

        var result = new GetUserFunctionRequest.Types.Result
                     {
                         Code = rep.Code,
                         Message = rep.Message,
                         Content =
                         {
                             rep.Content?.Select(t => new GetUserFunctionRequest.Types.Result.Types.UserFunction
                                                      {
                                                          Functions = { t.Functions.Select(t2 => t2.Adapt<GetUserFunctionRequest.Types.Result.Types.Function>()) }
                                                      })
                         }
                     };

        return result;
    }

    [Permission("AUTH", PermissionType.Read)]
    public override async Task<GetUserRoleRequest.Types.Result> GetUserRole(GetUserRoleRequest request, ServerCallContext context)
    {
        var req = request.Adapt<GetUserRole>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<GetUserRoleRequest.Types.Result>();
    }

    [Permission("AUTH", PermissionType.Write)]
    public override async Task<Result> UpdateUser(UpdateUserRequest request, ServerCallContext context)
    {
        var req = request.Adapt<UpdateUser>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<Result>();
    }
}
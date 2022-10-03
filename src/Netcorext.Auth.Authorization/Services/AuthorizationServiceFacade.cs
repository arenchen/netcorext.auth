using Grpc.Core;
using Mapster;
using Netcorext.Auth.Attributes;
using Netcorext.Auth.Authorization.Services.Token.Commands;
using Netcorext.Auth.Authorization.Services.User.Commands;
using Netcorext.Auth.Authorization.Services.User.Queries;
using Netcorext.Auth.Enums;
using Netcorext.Auth.Protobufs;
using Netcorext.Contracts.Protobufs;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authorization.Services.Authorization;

[Permission("AUTH", PermissionType.Write)]
public class AuthorizationServiceFacade : AuthorizationService.AuthorizationServiceBase
{
    private readonly IDispatcher _dispatcher;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthorizationServiceFacade(IDispatcher dispatcher, IHttpContextAccessor httpContextAccessor)
    {
        _dispatcher = dispatcher;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<CreateTokenRequest.Types.Result> CreateToken(CreateTokenRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<CreateToken>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<CreateTokenRequest.Types.Result>();
    }

    public override async Task<ExternalSignInRequest.Types.Result> ExternalSignIn(ExternalSignInRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<ExternalSignIn>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<ExternalSignInRequest.Types.Result>();
    }

    public override async Task<Result> ResetOtp(ResetOtpRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<ResetOtp>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<Result>();
    }

    public override async Task<Result> ResetPassword(ResetPasswordRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<ResetPassword>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<Result>();
    }

    public override async Task<SignInRequest.Types.Result> SignIn(SignInRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<SignIn>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<SignInRequest.Types.Result>();
    }

    public override async Task<Result> SignOut(SignOutRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<SignOut>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<Result>();
    }

    [Permission("AUTH", PermissionType.Read)]
    public override async Task<Result> ValidateOtp(ValidateOtpRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<ValidateOtp>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<Result>();
    }

    [Permission("AUTH", PermissionType.Read)]
    public override async Task<Result> ValidateUser(ValidateUserRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<ValidateUser>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<Result>();
    }
}
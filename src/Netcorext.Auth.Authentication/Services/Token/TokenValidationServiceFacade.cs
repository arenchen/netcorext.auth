using Grpc.Core;
using Mapster;
using Netcorext.Auth.Authentication.Services.Token.Queries;
using Netcorext.Auth.Protobufs;
using Netcorext.Contracts.Protobufs;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Token;

public class TokenValidationServiceFacade : TokenValidationService.TokenValidationServiceBase
{
    private readonly IDispatcher _dispatcher;

    public TokenValidationServiceFacade(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public override async Task<Result> ValidateToken(ValidateTokenRequest request, ServerCallContext context)
    {
        var req = request.Adapt<ValidateToken>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<Result>();
    }
}
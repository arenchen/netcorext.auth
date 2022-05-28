using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authorization.Services.User;

public class ResetOtp : IRequest<Result>
{
    public long? Id { get; set; }
    public string? Username { get; set; }
}
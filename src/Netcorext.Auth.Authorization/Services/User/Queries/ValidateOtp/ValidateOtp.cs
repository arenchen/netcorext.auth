using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authorization.Services.User;

public class ValidateOtp : IRequest<Result>
{
    public string? Username { get; set; }
    public string? Otp { get; set; }
}
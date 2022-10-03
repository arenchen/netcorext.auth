using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authorization.Services.User.Queries;

public class ValidateOtp : IRequest<Result>
{
    public long? Id { get; set; }
    public string? Username { get; set; }
    public string? Otp { get; set; }
}
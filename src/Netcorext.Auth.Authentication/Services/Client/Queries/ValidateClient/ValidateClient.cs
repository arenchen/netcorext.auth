using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Client;

public class ValidateClient : IRequest<Result>
{
    public long Id { get; set; }
    public string Secret { get; set; } = null!;
}
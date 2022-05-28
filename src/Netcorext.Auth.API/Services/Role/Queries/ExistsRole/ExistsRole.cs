using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role;

public class ExistsRole : IRequest<Result>
{
    public string Name { get; set; } = null!;
}
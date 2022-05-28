using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role;

public class DeleteRole : IRequest<Result>
{
    public long Id { get; set; }
}
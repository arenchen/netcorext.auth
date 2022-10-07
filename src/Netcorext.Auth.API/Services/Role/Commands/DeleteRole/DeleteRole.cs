using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role.Commands;

public class DeleteRole : IRequest<Result>
{
    public long[] Ids { get; set; } = null!;
}
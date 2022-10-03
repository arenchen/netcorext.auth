using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Permission.Commands;

public class DeletePermission : IRequest<Result>
{
    public long[] Ids { get; set; } = null!;
}
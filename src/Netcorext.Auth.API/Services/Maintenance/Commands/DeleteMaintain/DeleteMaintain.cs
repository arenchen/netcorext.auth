using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Maintenance.Commands;

public class DeleteMaintain : IRequest<Result>
{
    public string[] Keys { get; set; } = null!;
}

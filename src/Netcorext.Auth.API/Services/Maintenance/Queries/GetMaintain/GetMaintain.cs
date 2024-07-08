using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Maintenance.Queries;

public class GetMaintain : IRequest<Result<Dictionary<string, Models.MaintainItem>>>
{
    public Paging Paging { get; set; } = null!;
}

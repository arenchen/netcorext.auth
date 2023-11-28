using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserRole : IRequest<Result<IEnumerable<Models.FullUserRole>>>
{
    public long[] Ids { get; set; } = null!;
    public bool IncludeExtendData { get; set; }
    public bool IncludePermission { get; set; }
    public bool IncludePermissionCondition { get; set; }
    public bool IncludeExpired { get; set; }
}

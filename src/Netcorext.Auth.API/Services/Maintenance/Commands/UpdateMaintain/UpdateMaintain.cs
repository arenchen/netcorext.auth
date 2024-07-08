using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Maintenance.Commands;

public class UpdateMaintain : IRequest<Result>
{
    public Dictionary<string, MaintainItem> Items { get; set; } = null!;

    public class MaintainItem
    {
        public DateTimeOffset BeginDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public string? Message { get; set; }
        public string[]? ExcludeHosts { get; set; }
        public long[]? ExcludeRoles { get; set; }
    }
}

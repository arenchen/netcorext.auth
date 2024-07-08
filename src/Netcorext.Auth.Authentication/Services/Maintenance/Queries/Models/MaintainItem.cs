namespace Netcorext.Auth.Authentication.Services.Maintenance.Queries.Models;

public class MaintainItem
{
    public DateTimeOffset BeginDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public string? Message { get; set; }
    public string[]? ExcludeHosts { get; set; }
    public long[]? ExcludeRoles { get; set; }
}

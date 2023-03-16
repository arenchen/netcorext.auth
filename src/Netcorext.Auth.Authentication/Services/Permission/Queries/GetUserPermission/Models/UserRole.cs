namespace Netcorext.Auth.Authentication.Services.Permission.Queries.Models;

public class UserRole
{
    public long Id { get; set; }
    public long RoleId { get; set; }
    public DateTimeOffset? ExpireDate { get; set; }
}
using Netcorext.Auth.Enums;

namespace Netcorext.Auth.API.Services.User.Queries.Models;

public class Function
{
    public string Id { get; set; } = null!;
    public PermissionType PermissionType { get; set; }
}
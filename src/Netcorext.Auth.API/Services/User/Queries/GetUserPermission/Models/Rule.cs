using Netcorext.Auth.Enums;

namespace Netcorext.Auth.API.Services.User.Queries.Models;

public class Rule
{
    public long Id { get; set; }
    public string FunctionId { get; set; } = null!;
    public PermissionType PermissionType { get; set; }
    public bool Allowed { get; set; }
}
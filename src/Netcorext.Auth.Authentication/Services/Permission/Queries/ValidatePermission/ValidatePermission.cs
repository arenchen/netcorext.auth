using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Permission.Queries;

public class ValidatePermission : IRequest<Result>
{
    public long? UserId { get; set; }
    public long[]? RoleId { get; set; }
    public string FunctionId { get; set; } = null!;
    public string? Group { get; set; }
    public PermissionType PermissionType { get; set; }
    public PermissionCondition[]? PermissionConditions { get; set; }

    public class PermissionCondition
    {
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}
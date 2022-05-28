using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Permission;

public class ValidatePermission : IRequest<Result>
{
    public long? UserId { get; set; }
    public long[]? RoleId { get; set; }
    public string FunctionId { get; set; } = null!;
    public PermissionType PermissionType { get; set; }
    public PermissionExtendData[]? ExtendData { get; set; }

    public class PermissionExtendData
    {
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}
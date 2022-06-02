using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role;

public class CreateRole : IRequest<Result<IEnumerable<long>>>
{
    public Role[] Roles { get; set; } = null!;

    public class Role
    {
        public string Name { get; set; } = null!;
        public int Priority { get; set; }
        public bool Disabled { get; set; }
        public RoleExtendData[]? ExtendData { get; set; }
        public Permission[]? Permissions { get; set; }    
    }

    public class RoleExtendData
    {
        public string Key { get; set; } = null!;
        public string? Value { get; set; }
    }

    public class Permission
    {
        public string FunctionId { get; set; } = null!;
        public PermissionType PermissionType { get; set; }
        public bool Allowed { get; set; }
        public int Priority { get; set; }
        public bool ReplaceExtendData { get; set; }
        public DateTimeOffset? ExpireDate { get; set; }
        public PermissionExtendData[]? ExtendData { get; set; }
    }

    public class PermissionExtendData
    {
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
        public PermissionType PermissionType { get; set; }
        public bool Allowed { get; set; }
        
    }
}
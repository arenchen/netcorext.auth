using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role;

public class UpdateRole : IRequest<Result>
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public int? Priority { get; set; }
    public bool? Disabled { get; set; }
    public RoleExtendData[]? ExtendData { get; set; }
    public Permission[]? Permissions { get; set; }

    public class RoleExtendData
    {
        public CRUD CRUD { get; set; }
        public string Key { get; set; } = null!;
        public string? Value { get; set; }
    }

    public class Permission
    {
        public CRUD CRUD { get; set; }
        public long? Id { get; set; }
        public string? FunctionId { get; set; }
        public PermissionType? PermissionType { get; set; }
        public bool? Allowed { get; set; }
        public int? Priority { get; set; }
        public bool? ReplaceExtendData { get; set; }
        public DateTimeOffset? ExpireDate { get; set; }
        public PermissionExtendData[]? ExtendData { get; set; }
    }
    
    public class PermissionExtendData
    {
        public CRUD CRUD { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
        public PermissionType PermissionType { get; set; }
        public bool Allowed { get; set; }
    }
}
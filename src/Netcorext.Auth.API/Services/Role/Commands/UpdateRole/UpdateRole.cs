using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role.Commands;

public class UpdateRole : IRequest<Result>
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public int? Priority { get; set; }
    public bool? Disabled { get; set; }
    public RoleExtendData[]? ExtendData { get; set; }
    public RolePermission[]? Permissions { get; set; }
    public RolePermissionCondition[]? PermissionConditions { get; set; }

    public class RoleExtendData
    {
        public CRUD Crud { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }

    public class RolePermission
    {
        public CRUD Crud { get; set; }
        public long PermissionId { get; set; }
    }

    public class RolePermissionCondition
    {
        public CRUD Crud { get; set; }
        public long? Id { get; set; }
        public long PermissionId { get; set; }
        public string? Group { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}

using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role;

public class GetRole : IRequest<Result<IEnumerable<Models.Role>>>
{
    public long? Id { get; set; }
    public string? Name { get; set; }
    public int? Priority { get; set; }
    public bool? Disabled { get; set; }
    public RoleExtendData[]? ExtendData { get; set; }
    public RolePermission? Permission { get; set; }
    public Paging Paging { get; set; } = new();

    public class RoleExtendData
    {
        public string Key { get; set; } = null!;
        public string? Value { get; set; }
    }

    public class RolePermission
    {
        public string? FunctionId { get; set; }
        public PermissionType? PermissionType { get; set; }
        public bool? Allowed { get; set; }
        public int? Priority { get; set; }
        public bool? ReplaceExtendData { get; set; }
        public DateTimeOffset? ExpireDate { get; set; }
        public RolePermissionExtendData[]? ExtendData { get; set; }
    }

    public class RolePermissionExtendData
    {
        public string Key { get; set; } = null!;
        public string? Value { get; set; }
        public PermissionType? PermissionType { get; set; }
        public bool? Allowed { get; set; }
    }
}
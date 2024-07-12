using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role.Commands;

public class CloneRole : IRequest<Result<long?>>
{
    public long SourceId { get; set; }
    public string Name { get; set; } = null!;
    public int Priority { get; set; }
    public bool Disabled { get; set; }
    public RoleExtendData[]? ExtendData { get; set; }
    public RolePermission[]? Permissions { get; set; }
    public RolePermissionCondition[]? PermissionConditions { get; set; }
    public DefaultPermissionCondition[]? DefaultPermissionConditions { get; set; }
    public long? CustomId { get; set; }

    public class RoleExtendData
    {
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }

    public class RolePermission
    {
        public long PermissionId { get; set; }
    }

    public class RolePermissionCondition
    {
        public long PermissionId { get; set; }
        public string? Group { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }

    public class DefaultPermissionCondition
    {
        public string? Group { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}

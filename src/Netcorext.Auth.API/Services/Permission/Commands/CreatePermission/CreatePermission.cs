using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Permission.Commands;

public class CreatePermission : IRequest<Result<IEnumerable<long>>>
{
    public Permission[] Permissions { get; set; } = null!;

    public class Permission
    {
        public string Name { get; set; } = null!;
        public int Priority { get; set; }
        public bool Disabled { get; set; }
        public Rule[]? Rules { get; set; }
    }

    public class Rule
    {
        public string FunctionId { get; set; } = null!;
        public PermissionType PermissionType { get; set; }
        public bool Allowed { get; set; }
    }
}
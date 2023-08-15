using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Permission.Commands;

public class UpdatePermission : IRequest<Result>
{
    public long Id { get; set; }
    public string? Name { get; set; } = null!;
    public int? Priority { get; set; }
    public bool? Disabled { get; set; }
    public string? State { get; set; }
    public Rule[]? Rules { get; set; }

    public class Rule
    {
        public CRUD Crud { get; set; }
        public long? Id { get; set; }
        public string FunctionId { get; set; } = null!;
        public PermissionType PermissionType { get; set; }
        public bool Allowed { get; set; }
    }
}
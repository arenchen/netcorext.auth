using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role.Queries;

public class GetRoleFunction : IRequest<Result<IEnumerable<Models.RoleFunction>>>
{
    public long[] Ids { get; set; } = null!;
    public PermissionCondition[]? PermissionConditions { get; set; }

    public class PermissionCondition
    {
        public string? Group { get; set; }
        public Condition[]? Conditions { get; set; }

        public class Condition
        {
            public string Key { get; set; } = null!;
            public string Value { get; set; } = null!;
        }
    }
}
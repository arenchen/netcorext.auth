using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserPermission : IRequest<Result<IEnumerable<Models.UserPermission>>>
{
    public long Id { get; set; }
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
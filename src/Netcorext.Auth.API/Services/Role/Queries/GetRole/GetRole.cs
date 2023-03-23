using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role.Queries;

public class GetRole : IRequest<Result<IEnumerable<Models.Role>>>
{
    public long[]? Ids { get; set; }
    public string? Name { get; set; }
    public bool? Disabled { get; set; }
    public RoleExtendData[]? ExtendData { get; set; }
    public Paging Paging { get; set; } = new();

    public class RoleExtendData
    {
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}
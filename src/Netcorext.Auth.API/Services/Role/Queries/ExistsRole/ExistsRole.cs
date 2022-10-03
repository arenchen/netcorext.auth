using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role.Queries;

public class ExistsRole : IRequest<Result>
{
    public long? Id { get; set; }
    public string Name { get; set; } = null!;
}
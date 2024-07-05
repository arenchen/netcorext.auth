using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Maintenance.Commands;

public class Maintain : IRequest<Result>
{
    public bool Enabled { get; set; }
    public string? Message { get; set; }
    public string[]? ExcludeHosts { get; set; }
    public long[]? ExcludeRoles { get; set; }
}

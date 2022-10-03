using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Client.Commands;

public class DeleteClient : IRequest<Result>
{
    public long Id { get; set; }
}
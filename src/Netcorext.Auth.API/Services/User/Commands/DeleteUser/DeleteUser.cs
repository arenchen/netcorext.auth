using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User;

public class DeleteUser : IRequest<Result>
{
    public long Id { get; set; }
}
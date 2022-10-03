using FluentValidation;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserPermissionValidator : AbstractValidator<GetUserPermission>
{
    public GetUserPermissionValidator()
    {
        RuleFor(t => t.Id).NotEmpty();
    }
}
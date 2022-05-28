using FluentValidation;

namespace Netcorext.Auth.API.Services.User;

public class GetUserPermissionValidator : AbstractValidator<GetUserPermission>
{
    public GetUserPermissionValidator()
    {
        RuleFor(t => t.Id).NotEmpty();
    }
}
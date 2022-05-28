using FluentValidation;

namespace Netcorext.Auth.API.Services.User;

public class GetUserRoleValidator : AbstractValidator<GetUserRole>
{
    public GetUserRoleValidator()
    {
        RuleFor(t => t.Id).NotEmpty();
    }
}
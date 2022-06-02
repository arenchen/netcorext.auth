using FluentValidation;

namespace Netcorext.Auth.API.Services.Role;

public class DeleteRoleValidator : AbstractValidator<DeleteRole>
{
    public DeleteRoleValidator()
    {
        RuleFor(t => t.Ids).NotEmpty();
    }
}
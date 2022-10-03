using FluentValidation;

namespace Netcorext.Auth.API.Services.Role.Commands;

public class DeleteRoleValidator : AbstractValidator<DeleteRole>
{
    public DeleteRoleValidator()
    {
        RuleFor(t => t.Ids).NotEmpty();
    }
}
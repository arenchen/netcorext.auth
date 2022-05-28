using FluentValidation;

namespace Netcorext.Auth.API.Services.Role;

public class ExistsRoleValidator : AbstractValidator<ExistsRole>
{
    public ExistsRoleValidator()
    {
        RuleFor(t => t.Name).NotEmpty();
    }
}
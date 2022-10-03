using FluentValidation;

namespace Netcorext.Auth.API.Services.Role.Queries;

public class ExistsRoleValidator : AbstractValidator<ExistsRole>
{
    public ExistsRoleValidator()
    {
        RuleFor(t => t.Name).NotEmpty();
    }
}
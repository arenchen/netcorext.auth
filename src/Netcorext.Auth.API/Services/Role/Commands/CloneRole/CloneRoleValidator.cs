using FluentValidation;

namespace Netcorext.Auth.API.Services.Role.Commands;

public class CloneRoleValidator : AbstractValidator<CloneRole>
{
    public CloneRoleValidator()
    {
        RuleFor(t => t.SourceId).NotEmpty();
        RuleFor(t => t.Name).NotEmpty();
    }
}
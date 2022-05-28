using FluentValidation;

namespace Netcorext.Auth.API.Services.Role;

public class CreateRoleValidator : AbstractValidator<CreateRole>
{
    public CreateRoleValidator()
    {
        RuleFor(t => t.Name).NotEmpty();

        RuleForEach(t => t.Permissions).ChildRules(c =>
                                                   {
                                                       c.RuleFor(t => t.FunctionId).NotEmpty();
                                                       c.RuleFor(t => t.PermissionType).IsInEnum().NotEmpty();
                                                       c.RuleFor(t => t.ExpireDate).GreaterThan(t => DateTimeOffset.UtcNow).When(t => t.ExpireDate.HasValue);
                                                   });
    }
}
using FluentValidation;

namespace Netcorext.Auth.API.Services.Role;

public class CreateRoleValidator : AbstractValidator<CreateRole>
{
    public CreateRoleValidator()
    {
        RuleFor(t => t.Roles).NotEmpty();

        RuleForEach(t => t.Roles).ChildRules(c =>
                                             {
                                                 c.RuleFor(t => t.Name).NotEmpty();

                                                 c.RuleForEach(t2 => t2.Permissions).ChildRules(c2 =>
                                                                                                {
                                                                                                    c2.RuleFor(t => t.FunctionId).NotEmpty();
                                                                                                    c2.RuleFor(t => t.PermissionType).IsInEnum().NotEmpty();
                                                                                                    c2.RuleFor(t => t.ExpireDate).GreaterThan(t => DateTimeOffset.UtcNow).When(t => t.ExpireDate.HasValue);
                                                                                                });
                                             });
    }
}
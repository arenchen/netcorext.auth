using FluentValidation;

namespace Netcorext.Auth.API.Services.Permission.Commands;

public class CreatePermissionValidator : AbstractValidator<CreatePermission>
{
    public CreatePermissionValidator()
    {
        RuleFor(t => t.Permissions).NotEmpty();

        RuleForEach(t => t.Permissions)
           .ChildRules(c =>
                       {
                           c.RuleFor(t => t.Name).NotEmpty();

                           c.RuleForEach(t => t.Rules)
                            .ChildRules(c2 =>
                                        {
                                            c2.RuleFor(t => t.FunctionId).NotEmpty();
                                            c2.RuleFor(t => t.PermissionType).IsInEnum();
                                        });
                       });
    }
}
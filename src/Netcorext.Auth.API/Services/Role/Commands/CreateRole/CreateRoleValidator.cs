using FluentValidation;

namespace Netcorext.Auth.API.Services.Role.Commands;

public class CreateRoleValidator : AbstractValidator<CreateRole>
{
    public CreateRoleValidator()
    {
        RuleFor(t => t.Roles).NotEmpty();

        RuleForEach(t => t.Roles).ChildRules(c =>
                                             {
                                                 c.RuleFor(t => t.Name).NotEmpty();

                                                 c.RuleForEach(t2 => t2.ExtendData).ChildRules(c2 =>
                                                                                               {
                                                                                                   c2.RuleFor(t => t.Key).NotEmpty();
                                                                                                   c2.RuleFor(t => t.Value).NotEmpty();
                                                                                               });

                                                 c.RuleForEach(t2 => t2.PermissionConditions).ChildRules(c2 =>
                                                                                                         {
                                                                                                             c2.RuleFor(t => t.Key).NotEmpty();
                                                                                                             c2.RuleFor(t => t.Value).NotEmpty();
                                                                                                         });
                                             });
    }
}
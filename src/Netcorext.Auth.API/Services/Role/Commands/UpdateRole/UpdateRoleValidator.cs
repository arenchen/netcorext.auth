using FluentValidation;

namespace Netcorext.Auth.API.Services.Role.Commands;

public class UpdateRoleValidator : AbstractValidator<UpdateRole>
{
    public UpdateRoleValidator()
    {
        RuleForEach(t => t.ExtendData).ChildRules(c =>
                                                  {
                                                      c.RuleFor(t => t.Key).NotEmpty();
                                                      c.RuleFor(t => t.Value).NotEmpty();
                                                  });

        RuleForEach(t => t.PermissionConditions).ChildRules(c =>
                                                            {
                                                                c.RuleFor(t => t.Key).NotEmpty();
                                                                c.RuleFor(t => t.Value).NotEmpty();
                                                            });
    }
}
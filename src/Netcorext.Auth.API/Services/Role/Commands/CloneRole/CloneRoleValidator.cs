using FluentValidation;

namespace Netcorext.Auth.API.Services.Role.Commands;

public class CloneRoleValidator : AbstractValidator<CloneRole>
{
    public CloneRoleValidator()
    {
        RuleFor(t => t.SourceId).NotEmpty();
        RuleFor(t => t.Name).NotEmpty();

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

        RuleForEach(t => t.DefaultPermissionConditions).ChildRules(c =>
                                                                   {
                                                                       c.RuleFor(t => t.Key).NotEmpty();
                                                                       c.RuleFor(t => t.Value).NotEmpty();
                                                                   });
    }
}
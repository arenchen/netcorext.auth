using FluentValidation;

namespace Netcorext.Auth.API.Services.User.Commands;

public class UpdateUserValidator : AbstractValidator<UpdateUser>
{
    public UpdateUserValidator()
    {
        RuleFor(t => t.Id).NotEmpty();

        RuleForEach(t => t.ExtendData).ChildRules(c =>
                                                  {
                                                      c.RuleFor(t => t.Key).NotEmpty();
                                                  });

        RuleForEach(t => t.ExternalLogins).ChildRules(c =>
                                                      {
                                                          c.RuleFor(t => t.Provider).NotEmpty();
                                                          c.RuleFor(t => t.UniqueId).NotEmpty();
                                                      });

        RuleForEach(t => t.PermissionConditions).ChildRules(c =>
                                                            {
                                                                c.RuleFor(t => t.Key).NotEmpty();
                                                                c.RuleFor(t => t.Value).NotEmpty();
                                                            });
    }
}
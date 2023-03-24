using FluentValidation;

namespace Netcorext.Auth.API.Services.Client.Commands;

public class CreateClientValidator : AbstractValidator<CreateClient>
{
    public CreateClientValidator()
    {
        RuleFor(t => t.Name).NotEmpty();
        RuleFor(t => t.Secret).NotEmpty();
        RuleForEach(t => t.ExtendData).ChildRules(c =>
                                                  {
                                                      c.RuleFor(t => t.Key).NotEmpty();
                                                      c.RuleFor(t => t.Value).NotEmpty();
                                                  });
    }
}
using FluentValidation;

namespace Netcorext.Auth.API.Services.Client.Commands;

public class UpdateClientValidator : AbstractValidator<UpdateClient>
{
    public UpdateClientValidator()
    {
        RuleFor(t => t.Id).NotEmpty();
        RuleForEach(t => t.ExtendData).ChildRules(c =>
                                                  {
                                                      c.RuleFor(t => t.Key).NotEmpty();
                                                      c.RuleFor(t => t.Value).NotEmpty();
                                                  });
    }
}
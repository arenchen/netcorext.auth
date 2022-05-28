using FluentValidation;

namespace Netcorext.Auth.Authentication.Services.Client;

public class ValidateClientValidator : AbstractValidator<ValidateClient>
{
    public ValidateClientValidator()
    {
        RuleFor(t => t.Id).NotEmpty();
        RuleFor(t => t.Secret).NotEmpty();
    }
}
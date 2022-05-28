using FluentValidation;

namespace Netcorext.Auth.Authentication.Services.Token;

public class ValidateTokenValidator : AbstractValidator<ValidateToken>
{
    public ValidateTokenValidator()
    {
        RuleFor(t => t.Token).NotEmpty();
    }
}
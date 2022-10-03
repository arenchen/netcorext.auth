using FluentValidation;

namespace Netcorext.Auth.Authentication.Services.Token.Queries;

public class ValidateTokenValidator : AbstractValidator<ValidateToken>
{
    public ValidateTokenValidator()
    {
        RuleFor(t => t.Token).NotEmpty();
    }
}
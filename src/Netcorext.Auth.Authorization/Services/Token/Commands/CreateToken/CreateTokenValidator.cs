using FluentValidation;

namespace Netcorext.Auth.Authorization.Services.Token.Commands;

public class CreateTokenValidator : AbstractValidator<CreateToken>
{
    public CreateTokenValidator()
    {
        RuleFor(t => t.ClientId).NotEmpty();
        RuleFor(t => t.ClientSecret).NotEmpty();
    }
}
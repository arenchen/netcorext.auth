using FluentValidation;

namespace Netcorext.Auth.API.Services.Client;

public class CreateClientValidator : AbstractValidator<CreateClient>
{
    public CreateClientValidator()
    {
        RuleFor(t => t.Name).NotEmpty();
        RuleFor(t => t.Secret).NotEmpty();
    }
}
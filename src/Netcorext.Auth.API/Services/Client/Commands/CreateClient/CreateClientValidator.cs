using FluentValidation;

namespace Netcorext.Auth.API.Services.Client.Commands;

public class CreateClientValidator : AbstractValidator<CreateClient>
{
    public CreateClientValidator()
    {
        RuleFor(t => t.Name).NotEmpty();
        RuleFor(t => t.Secret).NotEmpty();
    }
}
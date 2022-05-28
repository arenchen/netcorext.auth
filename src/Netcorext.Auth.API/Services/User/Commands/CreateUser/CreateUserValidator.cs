using FluentValidation;

namespace Netcorext.Auth.API.Services.User;

public class CreateUserValidator : AbstractValidator<CreateUser>
{
    public CreateUserValidator()
    {
        RuleFor(t => t.Username).NotEmpty();
        RuleFor(t => t.Password).NotEmpty();
    }
}
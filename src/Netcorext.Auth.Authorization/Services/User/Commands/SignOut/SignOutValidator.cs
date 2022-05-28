using FluentValidation;

namespace Netcorext.Auth.Authorization.Services.User;

public class SignOutValidator : AbstractValidator<SignOut>
{
    public SignOutValidator()
    {
        RuleFor(t => t.Token).NotEmpty();
    }
}
using FluentValidation;

namespace Netcorext.Auth.Authorization.Services.User.Commands;

public class ExternalSignInValidator : AbstractValidator<ExternalSignIn>
{
    public ExternalSignInValidator()
    {
        RuleFor(t => t.Username).NotEmpty();
        RuleFor(t => t.Provider).NotEmpty();
        RuleFor(t => t.UniqueId).NotEmpty();
    }
}
using FluentValidation;

namespace Netcorext.Auth.Authorization.Services.User;

public class ExternalSignInValidator : AbstractValidator<ExternalSignIn>
{
    public ExternalSignInValidator()
    {
        RuleFor(t => t.Provider).NotEmpty();
        RuleFor(t => t.UniqueId).NotEmpty();
    }
}
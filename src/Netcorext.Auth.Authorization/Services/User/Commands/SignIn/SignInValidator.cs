using System.Text.RegularExpressions;
using FluentValidation;
using Netcorext.Extensions.Commons;

namespace Netcorext.Auth.Authorization.Services.User;

public class SignInValidator : AbstractValidator<SignIn>
{
    public SignInValidator()
    {
        RuleFor(t => t.Username).NotEmpty();
        RuleFor(t => t.Password).NotEmpty();
        RuleFor(t => t.Otp).Matches(t => new Regex("[0-9]+")).When(t => !t.Otp.IsEmpty());
    }
}
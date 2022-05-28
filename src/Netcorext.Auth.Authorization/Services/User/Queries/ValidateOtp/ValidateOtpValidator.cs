using FluentValidation;

namespace Netcorext.Auth.Authorization.Services.User;

public class ValidateOtpValidator : AbstractValidator<ValidateOtp>
{
    public ValidateOtpValidator()
    {
        RuleFor(t => t.Username).NotEmpty();
        RuleFor(t => t.Otp).NotEmpty();
    }
}
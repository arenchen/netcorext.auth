using FluentValidation;
using Netcorext.Extensions.Commons;

namespace Netcorext.Auth.Authorization.Services.User.Queries;

public class ValidateOtpValidator : AbstractValidator<ValidateOtp>
{
    public ValidateOtpValidator()
    {
        RuleFor(t => t.Id).NotEmpty().When(t => t.Username.IsEmpty());
        RuleFor(t => t.Username).NotEmpty().When(t => t.Id.IsEmpty());
        RuleFor(t => t.Otp).NotEmpty();
    }
}
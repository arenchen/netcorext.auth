using FluentValidation;
using Netcorext.Extensions.Commons;

namespace Netcorext.Auth.Authorization.Services.User;

public class ResetOtpValidator : AbstractValidator<ResetOtp>
{
    public ResetOtpValidator()
    {
        RuleFor(t => t.Id).NotEmpty().When(t => t.Username.IsEmpty());
        RuleFor(t => t.Username).NotEmpty().When(t => !t.Id.HasValue);
    }
}
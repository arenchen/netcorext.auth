using FluentValidation;

namespace Netcorext.Auth.Authorization.Services.User.Commands;

public class ResetPasswordValidator : AbstractValidator<ResetPassword>
{
    public ResetPasswordValidator()
    {
        RuleFor(t => t.Id).NotEmpty();
        RuleFor(t => t.Password).NotEmpty();
    }
}
using FluentValidation;
using Netcorext.Extensions.Commons;

namespace Netcorext.Auth.Authorization.Services.User;

public class ValidateUserValidator : AbstractValidator<ValidateUser>
{
    public ValidateUserValidator()
    {
        RuleFor(t => t.Username).NotEmpty().When(t => t.Id.IsEmpty());
        RuleFor(t => t.Id).NotEmpty().When(t => t.Username.IsEmpty());
        RuleFor(t => t.Password).NotEmpty();
    }
}
using FluentValidation;

namespace Netcorext.Auth.API.Services.User;

public class UpdateUserValidator : AbstractValidator<UpdateUser>
{
    public UpdateUserValidator()
    {
        RuleFor(t => t.Id).NotEmpty();
    }
}
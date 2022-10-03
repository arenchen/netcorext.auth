using FluentValidation;

namespace Netcorext.Auth.API.Services.User.Commands;

public class UpdateUserValidator : AbstractValidator<UpdateUser>
{
    public UpdateUserValidator()
    {
        RuleFor(t => t.Id).NotEmpty();
    }
}
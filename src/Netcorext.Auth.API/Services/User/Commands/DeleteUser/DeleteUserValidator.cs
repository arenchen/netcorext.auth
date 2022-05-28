using FluentValidation;

namespace Netcorext.Auth.API.Services.User;

public class DeleteUserValidator : AbstractValidator<DeleteUser>
{
    public DeleteUserValidator()
    {
        RuleFor(t => t.Id).NotEmpty();
    }
}
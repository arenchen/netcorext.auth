using FluentValidation;

namespace Netcorext.Auth.API.Services.User;

public class ExistsUserValidator : AbstractValidator<ExistsUser>
{
    public ExistsUserValidator()
    {
        RuleFor(t => t.Username).NotEmpty();
    }
}
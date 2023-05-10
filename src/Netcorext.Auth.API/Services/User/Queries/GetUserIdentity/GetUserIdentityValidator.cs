using FluentValidation;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserIdentityValidator : AbstractValidator<GetUserIdentity>
{
    public GetUserIdentityValidator()
    {
        RuleFor(t => t.Ids)
           .NotEmpty()
           .When(t => t.Usernames == null || t.Usernames.Length == 0);

        RuleFor(t => t.Usernames)
           .NotEmpty()
           .When(t => t.Ids == null || t.Ids.Length == 0);
    }
}
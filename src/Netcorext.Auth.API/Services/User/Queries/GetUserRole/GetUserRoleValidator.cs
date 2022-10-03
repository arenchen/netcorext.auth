using FluentValidation;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserRoleValidator : AbstractValidator<GetUserRole>
{
    public GetUserRoleValidator()
    {
        RuleFor(t => t.Ids).NotEmpty();
    }
}
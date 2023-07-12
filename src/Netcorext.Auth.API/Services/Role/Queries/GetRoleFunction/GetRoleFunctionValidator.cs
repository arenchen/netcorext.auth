using FluentValidation;

namespace Netcorext.Auth.API.Services.Role.Queries;

public class GetRoleFunctionValidator : AbstractValidator<GetRoleFunction>
{
    public GetRoleFunctionValidator()
    {
        RuleFor(t => t.Ids).NotEmpty();
    }
}
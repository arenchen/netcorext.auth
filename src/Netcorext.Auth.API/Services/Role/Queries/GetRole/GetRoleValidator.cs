using FluentValidation;

namespace Netcorext.Auth.API.Services.Role;

public class GetRoleValidator : AbstractValidator<GetRole>
{
    public GetRoleValidator()
    {
        RuleFor(t => t.Paging.Offset).GreaterThanOrEqualTo(0);
        RuleFor(t => t.Paging.Limit).GreaterThan(0);
    }
}
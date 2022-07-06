using FluentValidation;
using Netcorext.Extensions.Commons;

namespace Netcorext.Auth.API.Services.Role;

public class GetRoleValidator : AbstractValidator<GetRole>
{
    public GetRoleValidator()
    {
        RuleFor(t => t.Paging).NotNull();
        RuleFor(t => t.Paging.Offset).GreaterThanOrEqualTo(0).When(t => !t.Paging.IsNull());
        RuleFor(t => t.Paging.Limit).GreaterThan(0).When(t => !t.Paging.IsNull());
    }
}
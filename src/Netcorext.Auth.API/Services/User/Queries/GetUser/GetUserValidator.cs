using FluentValidation;

namespace Netcorext.Auth.API.Services.User;

public class GetUserValidator : AbstractValidator<GetUser>
{
    public GetUserValidator()
    {
        RuleFor(t => t.Paging).NotNull();
        RuleFor(t => t.Paging.Offset).GreaterThanOrEqualTo(0);
        RuleFor(t => t.Paging.Limit).GreaterThan(0);
    }
}
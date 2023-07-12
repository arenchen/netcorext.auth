using FluentValidation;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserFunctionValidator : AbstractValidator<GetUserFunction>
{
    public GetUserFunctionValidator()
    {
        RuleFor(t => t.Id).NotEmpty();
    }
}
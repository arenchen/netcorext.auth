using FluentValidation;
using Netcorext.Extensions.Commons;

namespace Netcorext.Auth.API.Services.Client;

public class GetClientValidator : AbstractValidator<GetClient>
{
    public GetClientValidator()
    {
        RuleFor(t => t.Paging).NotNull();
        RuleFor(t => t.Paging.Offset).GreaterThanOrEqualTo(0).When(t => !t.Paging.IsNull());
        RuleFor(t => t.Paging.Limit).GreaterThan(0).When(t => !t.Paging.IsNull());
    }
}
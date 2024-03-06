using FluentValidation;

namespace Netcorext.Auth.Authorization.Services.Token.Commands;

public class RevokeTokenValidator : AbstractValidator<RevokeToken>
{
    public RevokeTokenValidator()
    {
        RuleFor(t => t.ResourceId).NotEmpty().When(t => string.IsNullOrWhiteSpace(t.Token));
        RuleFor(t => t.Token).NotEmpty().When(t => string.IsNullOrWhiteSpace(t.ResourceId));
    }
}

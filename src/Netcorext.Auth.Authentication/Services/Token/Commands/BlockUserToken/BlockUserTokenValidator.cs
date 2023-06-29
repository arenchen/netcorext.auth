using FluentValidation;

namespace Netcorext.Auth.Authentication.Services.Token.Commands;

public class BlockUserTokenValidator : AbstractValidator<BlockUserToken>
{
    public BlockUserTokenValidator()
    {
        RuleFor(t => t.Ids).NotEmpty();
    }
}
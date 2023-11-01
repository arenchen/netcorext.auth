using FluentValidation;

namespace Netcorext.Auth.API.Services.Blocked.Commands;

public class DeleteBlockedIpValidator : AbstractValidator<DeleteBlockedIp>
{
    public DeleteBlockedIpValidator()
    {
        RuleFor(t => t.Ids).NotEmpty();
    }
}

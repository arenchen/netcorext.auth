using FluentValidation;

namespace Netcorext.Auth.API.Services.Blocked.Commands;

public class CreateBlockedIpValidator : AbstractValidator<CreateBlockedIp>
{
    public CreateBlockedIpValidator()
    {
        RuleFor(t => t.BlockedIps).NotEmpty();
    }
}

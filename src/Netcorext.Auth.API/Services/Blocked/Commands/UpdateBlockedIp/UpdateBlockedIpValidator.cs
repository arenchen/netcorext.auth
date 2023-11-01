using FluentValidation;

namespace Netcorext.Auth.API.Services.Blocked.Commands;

public class UpdateBlockedIpValidator : AbstractValidator<UpdateBlockedIp>
{
    public UpdateBlockedIpValidator()
    {
        RuleFor(t => t.Id).NotEmpty();
        RuleFor(t => t.Cidr).NotEmpty();
    }
}

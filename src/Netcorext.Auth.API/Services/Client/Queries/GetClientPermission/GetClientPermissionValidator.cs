using FluentValidation;

namespace Netcorext.Auth.API.Services.Client.Queries;

public class GetClientPermissionValidator : AbstractValidator<GetClientPermission>
{
    public GetClientPermissionValidator()
    {
        RuleFor(t => t.Id).NotEmpty();
    }
}
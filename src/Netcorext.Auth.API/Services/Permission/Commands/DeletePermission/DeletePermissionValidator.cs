using FluentValidation;

namespace Netcorext.Auth.API.Services.Permission.Commands;

public class DeletePermissionValidator : AbstractValidator<DeletePermission>
{
    public DeletePermissionValidator()
    {
        RuleFor(t => t.Ids).NotEmpty();
    }
}
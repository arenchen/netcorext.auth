using FluentValidation;

namespace Netcorext.Auth.API.Services.Role;

public class UpdateRoleValidator : AbstractValidator<UpdateRole>
{
    public UpdateRoleValidator()
    {
        RuleFor(t => t.Name).NotEmpty();

        RuleForEach(t => t.Permissions).ChildRules(c =>
                                                   {
                                                       c.RuleFor(t => t.FunctionId).NotEmpty();
                                                       c.RuleFor(t => t.PermissionType).IsInEnum().NotEmpty();
                                                   });
    }
}
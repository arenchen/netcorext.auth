using FluentValidation;
using Netcorext.Auth.Enums;

namespace Netcorext.Auth.API.Services.Permission.Commands;

public class UpdatePermissionValidator : AbstractValidator<UpdatePermission>
{
    public UpdatePermissionValidator()
    {
        RuleFor(t => t.Id).NotEmpty();

        RuleForEach(t => t.Rules)
           .ChildRules(c =>
                       {
                           c.RuleFor(t => t.Id).NotEmpty().When(t => t.Crud != CRUD.C);
                           c.RuleFor(t => t.FunctionId).NotEmpty().When(t => t.Crud != CRUD.D);
                           c.RuleFor(t => t.PermissionType).IsInEnum().When(t => t.Crud != CRUD.D);
                       });
    }
}
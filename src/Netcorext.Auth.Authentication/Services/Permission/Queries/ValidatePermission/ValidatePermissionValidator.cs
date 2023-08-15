using FluentValidation;
using Netcorext.Extensions.Commons;

namespace Netcorext.Auth.Authentication.Services.Permission.Queries;

public class ValidatePermissionValidator : AbstractValidator<ValidatePermission>
{
    public ValidatePermissionValidator()
    {
        RuleFor(t => t.UserId).NotEmpty().When(t => t.RoleId == null || !t.RoleId.Any());
        RuleFor(t => t.RoleId).NotEmpty().When(t => t.UserId.IsEmpty());
        RuleFor(t => t.FunctionId).NotEmpty();
        RuleFor(t => t.PermissionType).IsInEnum();

        RuleForEach(t => t.RoleExtendData).ChildRules(c =>
                                                      {
                                                          c.RuleFor(t => t.Key).NotEmpty();
                                                          c.RuleFor(t => t.Value).NotEmpty();
                                                      });
    }
}
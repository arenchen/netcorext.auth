using FluentValidation;

namespace Netcorext.Auth.Authentication.Services.Permission.Queries;

public class GetUserPermissionValidator : AbstractValidator<GetUserPermission>
{
    public GetUserPermissionValidator() { }
}
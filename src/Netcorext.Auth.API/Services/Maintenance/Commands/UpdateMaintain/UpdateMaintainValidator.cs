using FluentValidation;

namespace Netcorext.Auth.API.Services.Maintenance.Commands;

public class UpdateMaintainValidator : AbstractValidator<UpdateMaintain>
{
    public UpdateMaintainValidator()
    {
        RuleFor(t => t.Items).NotEmpty();
    }
}

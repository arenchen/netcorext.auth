using FluentValidation;

namespace Netcorext.Auth.API.Services.Maintenance.Commands;

public class CreateMaintainValidator : AbstractValidator<CreateMaintain>
{
    public CreateMaintainValidator()
    {
        RuleFor(t => t.Items).NotEmpty();
    }
}

using FluentValidation;

namespace Netcorext.Auth.API.Services.Maintenance.Commands;

public class DeleteMaintainValidator : AbstractValidator<DeleteMaintain>
{
    public DeleteMaintainValidator()
    {
        RuleFor(t => t.Keys).NotEmpty();
    }
}

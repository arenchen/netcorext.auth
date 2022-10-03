using FluentValidation;

namespace Netcorext.Auth.API.Services.Client.Commands;

public class UpdateClientValidator : AbstractValidator<UpdateClient>
{
    public UpdateClientValidator()
    {
        RuleFor(t => t.Id).NotEmpty();
    }
}
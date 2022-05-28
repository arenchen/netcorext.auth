using FluentValidation;

namespace Netcorext.Auth.API.Services.Client;

public class UpdateClientValidator : AbstractValidator<UpdateClient>
{
    public UpdateClientValidator()
    {
        RuleFor(t => t.Id).NotEmpty();
    }
}
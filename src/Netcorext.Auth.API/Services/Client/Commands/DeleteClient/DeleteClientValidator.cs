using FluentValidation;

namespace Netcorext.Auth.API.Services.Client.Commands;

public class DeleteClientValidator : AbstractValidator<DeleteClient>
{
    public DeleteClientValidator()
    {
        RuleFor(t => t.Id).NotEmpty();
    }
}
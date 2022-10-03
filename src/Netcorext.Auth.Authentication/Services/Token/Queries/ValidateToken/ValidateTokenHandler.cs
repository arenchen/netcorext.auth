using Microsoft.EntityFrameworkCore;
using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Token.Queries;

public class ValidateTokenHandler : IRequestHandler<ValidateToken, Result>
{
    private readonly DatabaseContext _context;

    public ValidateTokenHandler(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(ValidateToken request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Token>();
        var dsUser = _context.Set<Domain.Entities.User>();
        var dsClient = _context.Set<Domain.Entities.Client>();

        if (!await ds.AnyAsync(t => t.AccessToken == request.Token || t.RefreshToken == request.Token, cancellationToken)) return Result.InvalidInput;

        var entity = await ds.FirstAsync(t => t.AccessToken == request.Token || t.RefreshToken == request.Token, cancellationToken);

        if (entity.Disabled) return Result.InvalidInput;

        var isResourceValid = entity.ResourceType switch
                              {
                                  ResourceType.Client => await dsClient.AnyAsync(t => t.Id == long.Parse(entity.ResourceId) && !t.Disabled, cancellationToken),
                                  ResourceType.User => await dsUser.AnyAsync(t => t.Id == long.Parse(entity.ResourceId) && !t.Disabled, cancellationToken),
                                  _ => false
                              };

        return isResourceValid ? Result.Success : Result.InvalidInput;
    }
}
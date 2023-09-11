using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.EntityFramework.UserIdentityPattern.Extensions;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Token.Commands;

public class BlockUserTokenHandler : IRequestHandler<BlockUserToken, Result>
{
    private readonly DatabaseContext _context;

    public BlockUserTokenHandler(DatabaseContextAdapter context)
    {
        _context = context.Master;
    }

    public async Task<Result> Handle(BlockUserToken request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Token>();

        var idStrings = request.Ids.Select(id => id.ToString()).ToArray();

        var tokens = ds.Where(t => t.Revoked != TokenRevoke.Both && t.ResourceType == ResourceType.User && idStrings.Contains(t.ResourceId));

        foreach (var token in tokens)
        {
            _context.Entry(token).UpdateProperty(t => t.Revoked, TokenRevoke.Both);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}

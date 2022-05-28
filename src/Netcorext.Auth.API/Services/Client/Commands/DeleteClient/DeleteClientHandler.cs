using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Client;

public class DeleteClientHandler : IRequestHandler<DeleteClient, Result>
{
    private readonly DatabaseContext _context;

    public DeleteClientHandler(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteClient request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.Client>();
        var dsRole = _context.Set<Domain.Entities.ClientRole>();
        var dsExtendData = _context.Set<Domain.Entities.ClientExtendData>();

        var qClient = ds.Where(t => t.Id == request.Id);

        if (!qClient.Any()) return Result.Success;

        var qRole = dsRole.Where(t => t.Id == request.Id);

        if (qRole.Any()) dsRole.RemoveRange(qRole);

        var qExtendData = dsExtendData.Where(t => t.Id == request.Id);

        if (qExtendData.Any()) dsExtendData.RemoveRange(qExtendData);

        ds.RemoveRange(qClient);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
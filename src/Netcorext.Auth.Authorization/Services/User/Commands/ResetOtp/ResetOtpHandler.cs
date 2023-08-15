using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netcorext.Algorithms;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.EntityFramework.UserIdentityPattern.Extensions;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authorization.Services.User.Commands;

public class ResetOtpHandler : IRequestHandler<ResetOtp, Result>
{
    private readonly DatabaseContext _context;
    private readonly AuthOptions _config;

    public ResetOtpHandler(DatabaseContextAdapter context, IOptions<AuthOptions> config)
    {
        _context = context;
        _config = config.Value;
    }

    public async Task<Result> Handle(ResetOtp request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.User>();

        Expression<Func<Domain.Entities.User, bool>> predicate = u => true;

        if (request.Id.HasValue) predicate = predicate.And(t => t.Id == request.Id);

        if (!string.IsNullOrWhiteSpace(request.Username)) predicate = predicate.And(t => t.NormalizedUsername == request.Username.ToUpper());

        var entity = await ds.FirstOrDefaultAsync(predicate, cancellationToken);

        if (entity == null) return Result<string?>.NotFound;

        _context.Entry(entity).UpdateProperty(t => t.Otp, Otp.GenerateRandomKey().ToBase32String());
        _context.Entry(entity).UpdateProperty(t => t.OtpBound, false);

        await _context.SaveChangesAsync(cancellationToken);

        var otpAuthScheme = string.Format(_config.OtpAuthScheme, _config.Issuer, entity.Username, entity.Otp);

        return new Result
               {
                   Code = Result.Success,
                   Message = otpAuthScheme
               };
    }
}
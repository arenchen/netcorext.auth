using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Algorithms;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authorization.Services.User.Queries;

public class ValidateOtpHandler : IRequestHandler<ValidateOtp, Result>
{
    private readonly DatabaseContext _context;

    public ValidateOtpHandler(DatabaseContextAdapter context)
    {
        _context = context.Slave;
    }

    public async Task<Result> Handle(ValidateOtp request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.User>();

        Expression<Func<Domain.Entities.User, bool>> predicate = t => true;

        if (request.Id.HasValue)
            predicate = predicate.And(t => t.Id == request.Id);

        if (!request.Username.IsEmpty())
            predicate = predicate.And(t => t.NormalizedUsername == request.Username.ToUpper());

        if (!await ds.AnyAsync(predicate, cancellationToken))
            return Result.TwoFactorAuthenticationFailed;

        var entity = await ds.FirstAsync(predicate, cancellationToken);

        if (!entity.TwoFactorEnabled || !entity.OtpBound) return Result.RequiredTwoFactorAuthenticationBinding;

        return !Otp.ValidateCode(entity.Otp!, request.Otp!) ? Result.TwoFactorAuthenticationFailed : Result.Success;
    }
}
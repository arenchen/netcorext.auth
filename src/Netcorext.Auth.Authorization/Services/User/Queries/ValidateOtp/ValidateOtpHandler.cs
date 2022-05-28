using Microsoft.EntityFrameworkCore;
using Netcorext.Algorithms;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authorization.Services.User;

public class ValidateOtpHandler : IRequestHandler<ValidateOtp, Result>
{
    private readonly DatabaseContext _context;

    public ValidateOtpHandler(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(ValidateOtp request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.User>();

        if (!await ds.AnyAsync(t => t.NormalizedUsername == request.Username!.ToUpper(), cancellationToken)) return Result.TwoFactorAuthenticationFailed;

        var entity = await ds.FirstAsync(t => t.NormalizedUsername == request.Username!.ToUpper(), cancellationToken);

        if (!entity.TwoFactorEnabled || !entity.OtpBound) return Result.RequiredTwoFactorAuthenticationBinding;

        return !Otp.ValidateCode(entity.Otp!, request.Otp!) ? Result.TwoFactorAuthenticationFailed : Result.Success;
    }
}
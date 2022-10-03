using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserHandler : IRequestHandler<GetUser, Result<IEnumerable<Models.User>>>
{
    private readonly DatabaseContext _context;

    public GetUserHandler(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Result<IEnumerable<Models.User>>> Handle(GetUser request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.User>();

        Expression<Func<Domain.Entities.User, bool>> predicate = p => true;

        if (request.Ids != null && request.Ids.Any())
        {
            Expression<Func<Domain.Entities.User, bool>> predicateId = p => false;

            predicateId = request.Ids.Aggregate(predicateId, (current, id) => current.Or(t => t.Id == id));

            predicate = predicate.And(predicateId);
        }

        if (!request.Username.IsEmpty()) predicate = predicate.And(p => p.NormalizedUsername.Contains(request.Username!.ToUpper()));

        if (!request.Email.IsEmpty()) predicate = predicate.And(p => p.NormalizedEmail!.Contains(request.Email!.ToUpper()));

        if (request.EmailConfirmed.HasValue) predicate = predicate.And(p => p.EmailConfirmed == request.EmailConfirmed);

        if (!request.PhoneNumber.IsEmpty()) predicate = predicate.And(p => p.PhoneNumber!.Contains(request.PhoneNumber!));

        if (request.PhoneNumberConfirmed.HasValue) predicate = predicate.And(p => p.PhoneNumberConfirmed == request.PhoneNumberConfirmed);

        if (request.Disabled.HasValue) predicate = predicate.And(p => p.Disabled == request.Disabled);

        if (request.Role != null)
        {
            Expression<Func<Domain.Entities.UserRole, bool>> predicateRole = p => !p.Role.Disabled;

            if (request.Role.RoleId.HasValue) predicateRole = predicateRole.And(p => p.RoleId == request.Role.RoleId);
            if (!request.Role.Name.IsEmpty()) predicateRole = predicateRole.And(p => p.Role.Name.Contains(request.Role.Name));
            if (request.Role.ExpireDate.HasValue) predicateRole = predicateRole.And(p => p.ExpireDate == request.Role.ExpireDate);

            predicate = predicate.And(t => t.Roles.Any(predicateRole.Compile()));
        }

        if (!request.ExtendData.IsEmpty())
        {
            predicate = request.ExtendData.Aggregate(predicate, (expression, extendData) => expression.And(t => t.ExtendData.Any(t2 => t2.Key == extendData.Key.ToUpper() && t2.Value == extendData.Value)));
        }

        if (request.ExternalLogin != null)
        {
            Expression<Func<Domain.Entities.UserExternalLogin, bool>> predicateExternalLogin = p => true;

            if (!request.ExternalLogin.Provider.IsEmpty()) predicateExternalLogin = predicateExternalLogin.And(p => p.Provider == request.ExternalLogin.Provider);
            if (!request.ExternalLogin.UniqueId.IsEmpty()) predicateExternalLogin = predicateExternalLogin.And(p => p.UniqueId == request.ExternalLogin.UniqueId);

            predicate = predicate.And(t => t.ExternalLogins.Any(predicateExternalLogin.Compile()));
        }

        if (!request.Keyword.IsEmpty())
        {
            predicate = predicate.And(p => p.NormalizedUsername.Contains(request.Keyword!.ToUpper()) ||
                                           p.NormalizedEmail!.Contains(request.Keyword.ToUpper()) ||
                                           p.PhoneNumber!.Contains(request.Keyword.ToUpper()) ||
                                           p.ExtendData.Any(p2 => p2.Value!.ToUpper().Contains(request.Keyword.ToUpper())));
        }

        var queryEntities = ds.Include(t => t.ExtendData)
                              .Include(t => t.Roles).ThenInclude(t => t.Role)
                              .Include(t => t.ExternalLogins)
                              .Where(predicate)
                              .AsNoTracking();

        var pagination = await queryEntities
                              .GroupBy(t => 0)
                              .Select(t => new
                                           {
                                               Count = t.Count(),
                                               Rows = t.OrderBy(t2 => t2.Id)
                                                       .Skip(request.Paging.Offset)
                                                       .Take(request.Paging.Limit)
                                           })
                              .FirstOrDefaultAsync(cancellationToken);

        request.Paging.Count = pagination?.Count ?? 0;

        var content = pagination?.Rows.Select(t => new Models.User
                                                   {
                                                       Id = t.Id,
                                                       Username = t.Username,
                                                       Email = t.Email,
                                                       EmailConfirmed = t.EmailConfirmed,
                                                       PhoneNumber = t.PhoneNumber,
                                                       PhoneNumberConfirmed = t.PhoneNumberConfirmed,
                                                       Otp = t.Otp,
                                                       OtpBound = t.OtpBound,
                                                       TwoFactorEnabled = t.TwoFactorEnabled,
                                                       RequiredChangePassword = t.RequiredChangePassword,
                                                       TokenExpireSeconds = t.TokenExpireSeconds,
                                                       RefreshTokenExpireSeconds = t.RefreshTokenExpireSeconds,
                                                       CodeExpireSeconds = t.CodeExpireSeconds,
                                                       LastSignInDate = t.LastSignInDate,
                                                       LastSignInIp = t.LastSignInIp,
                                                       Disabled = t.Disabled,
                                                       CreationDate = t.CreationDate,
                                                       CreatorId = t.CreatorId,
                                                       ModificationDate = t.ModificationDate,
                                                       ModifierId = t.ModifierId,
                                                       Roles = t.Roles
                                                                .Where(t2 => !t2.Role.Disabled)
                                                                .Select(t2 => new Models.UserRole
                                                                              {
                                                                                  RoleId = t2.RoleId,
                                                                                  Name = t2.Role.Name,
                                                                                  ExpireDate = t2.ExpireDate,
                                                                                  CreationDate = t2.CreationDate,
                                                                                  CreatorId = t2.CreatorId,
                                                                                  ModificationDate = t2.ModificationDate,
                                                                                  ModifierId = t2.ModifierId
                                                                              }),
                                                       ExtendData = t.ExtendData.Select(t2 => new Models.UserExtendData
                                                                                              {
                                                                                                  Key = t2.Key,
                                                                                                  Value = t2.Value,
                                                                                                  CreationDate = t2.CreationDate,
                                                                                                  CreatorId = t2.CreatorId,
                                                                                                  ModificationDate = t2.ModificationDate,
                                                                                                  ModifierId = t2.ModifierId
                                                                                              }),
                                                       ExternalLogins = t.ExternalLogins.Select(t2 => new Models.UserExternalLogin
                                                                                                      {
                                                                                                          Provider = t2.Provider,
                                                                                                          UniqueId = t2.UniqueId,
                                                                                                          CreationDate = t2.CreationDate,
                                                                                                          CreatorId = t2.CreatorId,
                                                                                                          ModificationDate = t2.ModificationDate,
                                                                                                          ModifierId = t2.ModifierId
                                                                                                      })
                                                   })
                                 .ToArray();

        if (content != null && !content.Any()) content = null;

        return Result<IEnumerable<Models.User>>.Success.Clone(content, request.Paging);
    }
}
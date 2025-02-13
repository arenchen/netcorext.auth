using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Settings;
using Netcorext.Configuration.Extensions;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.User.Queries;

public class GetUserHandler : IRequestHandler<GetUser, Result<IEnumerable<Models.User>>>
{
    private readonly DatabaseContext _context;
    private readonly int _dataSizeLimit;

    public GetUserHandler(DatabaseContextAdapter context, IOptions<ConfigSettings> config)
    {
        _context = context.Slave;
        _dataSizeLimit = config.Value.Connections.RelationalDb.GetDefault().DataSizeLimit;
    }

    public async Task<Result<IEnumerable<Models.User>>> Handle(GetUser request, CancellationToken cancellationToken = default)
    {
        var ds = _context.Set<Domain.Entities.User>();

        Expression<Func<Domain.Entities.User, bool>> predicate = p => true;

        if (!request.Ids.IsEmpty()) predicate = predicate.And(t => request.Ids.Contains(t.Id));

        if (!request.Username.IsEmpty()) predicate = predicate.And(p => p.NormalizedUsername.Equals(request.Username!.ToUpper()));

        if (!request.DisplayName.IsEmpty()) predicate = predicate.And(p => p.NormalizedDisplayName.Equals(request.DisplayName!.ToUpper()));

        if (!request.Email.IsEmpty()) predicate = predicate.And(p => p.NormalizedEmail != null && p.NormalizedEmail.Equals(request.Email.ToUpper()));

        if (!request.EmailConfirmed.IsEmpty()) predicate = predicate.And(p => p.EmailConfirmed == request.EmailConfirmed);

        if (!request.PhoneNumber.IsEmpty()) predicate = predicate.And(p => p.PhoneNumber != null && p.PhoneNumber.Equals(request.PhoneNumber));

        if (!request.PhoneNumberConfirmed.IsEmpty()) predicate = predicate.And(p => p.PhoneNumberConfirmed == request.PhoneNumberConfirmed);

        if (!request.Verified.IsEmpty()) predicate = predicate.And(p => p.Verified == request.Verified);

        if (!request.Disabled.IsEmpty()) predicate = predicate.And(p => p.Disabled == request.Disabled);

        if (request.Role != null)
        {
            Expression<Func<Domain.Entities.UserRole, bool>> predicateRole = p => !p.Role.Disabled;

            if (request.Role.RoleId.HasValue) predicateRole = predicateRole.And(p => p.RoleId == request.Role.RoleId);
            if (!request.Role.Name.IsEmpty()) predicateRole = predicateRole.And(p => p.Role.Name.ToUpper().Contains(request.Role.Name.ToUpper()));
            if (request.Role.ExpireDate.HasValue) predicateRole = predicateRole.And(p => p.ExpireDate == request.Role.ExpireDate);

            predicate = predicate.And(t => t.Roles.AsQueryable().Any(predicateRole));
        }

        if (request.ExtendData != null && request.ExtendData.Any())
        {
            Expression<Func<Domain.Entities.UserExtendData, bool>> predicateExtendData = p => false;

            var extendData = request.ExtendData.GroupBy(t => t.Key, (k, values) =>
                                                                        new
                                                                        {
                                                                            Key = k.ToUpper(),
                                                                            Values = values.Select(t => t.Value.ToUpper())
                                                                        });

            predicateExtendData = extendData.Aggregate(predicateExtendData, (current, item) => current.Or(t => t.Key == item.Key && item.Values.Contains(t.Value)));

            predicate = predicate.And(t => t.ExtendData.AsQueryable().Any(predicateExtendData));
        }

        if (request.ExternalLogin != null)
        {
            Expression<Func<Domain.Entities.UserExternalLogin, bool>> predicateExternalLogin = p => true;

            if (!request.ExternalLogin.Provider.IsEmpty()) predicateExternalLogin = predicateExternalLogin.And(p => p.Provider == request.ExternalLogin.Provider);
            if (!request.ExternalLogin.UniqueId.IsEmpty()) predicateExternalLogin = predicateExternalLogin.And(p => p.UniqueId == request.ExternalLogin.UniqueId);

            predicate = predicate.And(t => t.ExternalLogins.AsQueryable().Any(predicateExternalLogin));
        }

        if (request.LastSignInIps != null && request.LastSignInIps.Any())
        {
            predicate = predicate.And(t => t.LastSignInIp != null && request.LastSignInIps.Contains(t.LastSignInIp));
        }

        var queryEntities = ds.AsNoTracking()
                              .Where(predicate)
                              .OrderBy(t => t.Id)
                              .Take(_dataSizeLimit);

        var pagination = await queryEntities.GroupBy(t => 0)
                                            .Select(t => new
                                                         {
                                                             Count = t.Count(),
                                                             Rows = t.OrderBy(t2 => t2.Id)
                                                                     .Skip(request.Paging.Offset)
                                                                     .Take(request.Paging.Limit)
                                                                     .Select(t2 => new Models.User
                                                                                   {
                                                                                       Id = t2.Id,
                                                                                       Username = t2.Username,
                                                                                       DisplayName = t2.DisplayName,
                                                                                       HasPassword = t2.Password != null && t2.Password != "",
                                                                                       Email = t2.Email,
                                                                                       EmailConfirmed = t2.EmailConfirmed,
                                                                                       PhoneNumber = t2.PhoneNumber,
                                                                                       PhoneNumberConfirmed = t2.PhoneNumberConfirmed,
                                                                                       Otp = t2.Otp,
                                                                                       OtpBound = t2.OtpBound,
                                                                                       TwoFactorEnabled = t2.TwoFactorEnabled,
                                                                                       RequiredChangePassword = t2.RequiredChangePassword,
                                                                                       AllowedRefreshToken = t2.AllowedRefreshToken,
                                                                                       TokenExpireSeconds = t2.TokenExpireSeconds,
                                                                                       RefreshTokenExpireSeconds = t2.RefreshTokenExpireSeconds,
                                                                                       CodeExpireSeconds = t2.CodeExpireSeconds,
                                                                                       LastSignInDate = t2.LastSignInDate,
                                                                                       LastSignInIp = t2.LastSignInIp,
                                                                                       Verified = t2.Verified,
                                                                                       Disabled = t2.Disabled,
                                                                                       CreationDate = t2.CreationDate,
                                                                                       CreatorId = t2.CreatorId,
                                                                                       ModificationDate = t2.ModificationDate,
                                                                                       ModifierId = t2.ModifierId,
                                                                                       Roles = t2.Roles
                                                                                                 .Where(t3 => !t3.Role.Disabled)
                                                                                                 .Select(t3 => new Models.UserRole
                                                                                                               {
                                                                                                                   RoleId = t3.RoleId,
                                                                                                                   Name = t3.Role.Name,
                                                                                                                   Priority = t3.Role.Priority,
                                                                                                                   ExpireDate = t3.ExpireDate,
                                                                                                                   CreationDate = t3.CreationDate,
                                                                                                                   CreatorId = t3.CreatorId,
                                                                                                                   ModificationDate = t3.ModificationDate,
                                                                                                                   ModifierId = t3.ModifierId
                                                                                                               }),
                                                                                       ExtendData = t2.ExtendData.Select(t3 => new Models.UserExtendData
                                                                                                                               {
                                                                                                                                   Key = t3.Key,
                                                                                                                                   Value = t3.Value,
                                                                                                                                   CreationDate = t3.CreationDate,
                                                                                                                                   CreatorId = t3.CreatorId,
                                                                                                                                   ModificationDate = t3.ModificationDate,
                                                                                                                                   ModifierId = t3.ModifierId
                                                                                                                               }),
                                                                                       ExternalLogins = t2.ExternalLogins.Select(t3 => new Models.UserExternalLogin
                                                                                                                                       {
                                                                                                                                           Provider = t3.Provider,
                                                                                                                                           UniqueId = t3.UniqueId,
                                                                                                                                           CreationDate = t3.CreationDate,
                                                                                                                                           CreatorId = t3.CreatorId,
                                                                                                                                           ModificationDate = t3.ModificationDate,
                                                                                                                                           ModifierId = t3.ModifierId
                                                                                                                                       }),
                                                                                       PermissionConditions = t2.PermissionConditions.Select(t3 => new Models.UserPermissionCondition
                                                                                                                                                   {
                                                                                                                                                       Id = t3.Id,
                                                                                                                                                       PermissionId = t3.PermissionId,
                                                                                                                                                       Group = t3.Group,
                                                                                                                                                       Key = t3.Key,
                                                                                                                                                       Value = t3.Value,
                                                                                                                                                       ExpireDate = t3.ExpireDate,
                                                                                                                                                       CreationDate = t3.CreationDate,
                                                                                                                                                       CreatorId = t3.CreatorId,
                                                                                                                                                       ModificationDate = t3.ModificationDate,
                                                                                                                                                       ModifierId = t3.ModifierId
                                                                                                                                                   })
                                                                                   })
                                                         })
                                            .SingleOrDefaultAsync(cancellationToken);

        request.Paging.Count = pagination?.Count ?? 0;

        var content = pagination?.Rows.ToArray();

        if (content != null && !content.Any()) content = null;

        return Result<IEnumerable<Models.User>>.Success.Clone(content, request.Paging);
    }
}

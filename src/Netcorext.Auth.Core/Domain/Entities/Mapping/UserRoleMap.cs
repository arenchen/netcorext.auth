using Microsoft.EntityFrameworkCore;
using Netcorext.EntityFramework.UserIdentityPattern.Entities.Mapping;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Netcorext.Auth.Domain.Entities.Mapping;

public class UserRoleMap : EntityMap<UserRole>
{
    public UserRoleMap(ModelBuilder modelBuilder) : base(modelBuilder)
    {
        Builder.HasKey(t => new { t.Id, t.RoleId });

        // Columns
        Builder.Property(t => t.RoleId)
               .HasColumnName(nameof(UserRole.RoleId));

        Builder.Property(t => t.ExpireDate)
               .HasColumnName(nameof(UserRole.ExpireDate));

        // Relationships
        Builder.HasOne(t => t.User)
               .WithMany(t => t.Roles)
               .HasForeignKey(t => t.Id);

        Builder.HasOne(t => t.Role)
               .WithMany(t => t.UserRoles)
               .HasForeignKey(t => t.RoleId);
    }
}

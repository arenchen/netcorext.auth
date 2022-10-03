using Microsoft.EntityFrameworkCore;
using Netcorext.EntityFramework.UserIdentityPattern.Entities.Mapping;

namespace Netcorext.Auth.Domain.Entities.Mapping;

public class RolePermissionMap : EntityMap<RolePermission>
{
    public RolePermissionMap(ModelBuilder modelBuilder) : base(modelBuilder)
    {
        // Key
        Builder.HasKey(t => new { t.Id, t.PermissionId });

        // Indexes
        Builder.HasIndex(t => t.PermissionId);

        // Properties
        Builder.Property(t => t.PermissionId)
               .HasColumnName(nameof(RolePermission.PermissionId))
               .HasMaxLength(50);

        // Relationships
        Builder.HasOne(t => t.Role)
               .WithMany(t => t.Permissions)
               .HasForeignKey(d => d.Id);

        Builder.HasOne(t => t.Permission)
               .WithMany(t => t.RolePermissions)
               .HasForeignKey(d => d.PermissionId);
    }
}
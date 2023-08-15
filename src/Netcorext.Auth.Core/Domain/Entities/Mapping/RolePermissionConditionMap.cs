using Microsoft.EntityFrameworkCore;
using Netcorext.EntityFramework.UserIdentityPattern.Entities.Mapping;

namespace Netcorext.Auth.Domain.Entities.Mapping;

public class RolePermissionConditionMap : EntityMap<RolePermissionCondition>
{
    public RolePermissionConditionMap(ModelBuilder modelBuilder) : base(modelBuilder)
    {
        // Indexes
        Builder.HasIndex(t => new { t.RoleId, t.PermissionId, t.Group, t.Key, t.Value })
               .IsUnique();

        Builder.HasIndex(t => t.RoleId);
        Builder.HasIndex(t => t.PermissionId);
        Builder.HasIndex(t => t.Group);

        // Columns
        Builder.Property(t => t.RoleId)
               .HasColumnName(nameof(RolePermissionCondition.RoleId));

        Builder.Property(t => t.PermissionId)
               .HasColumnName(nameof(RolePermissionCondition.PermissionId));

        Builder.Property(t => t.Group)
               .HasColumnName(nameof(RolePermissionCondition.Group))
               .HasMaxLength(50);

        Builder.Property(t => t.Key)
               .HasColumnName(nameof(RolePermissionCondition.Key))
               .HasMaxLength(50);

        Builder.Property(t => t.Value)
               .HasColumnName(nameof(RolePermissionCondition.Value))
               .HasMaxLength(200);

        // Relationships
        Builder.HasOne(t => t.Role)
               .WithMany(t => t.PermissionConditions)
               .HasForeignKey(t => t.RoleId);

        Builder.HasOne(t => t.Permission)
               .WithMany(t => t.RolePermissionConditions)
               .HasForeignKey(t => t.PermissionId);
    }
}
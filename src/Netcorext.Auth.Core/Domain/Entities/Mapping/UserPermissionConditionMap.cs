using Microsoft.EntityFrameworkCore;
using Netcorext.EntityFramework.UserIdentityPattern.Entities.Mapping;

namespace Netcorext.Auth.Domain.Entities.Mapping;

public class UserPermissionConditionMap : EntityMap<UserPermissionCondition>
{
    public UserPermissionConditionMap(ModelBuilder modelBuilder) : base(modelBuilder)
    {
        // Indexes
        Builder.HasIndex(t => new { t.UserId, t.PermissionId, t.Priority, t.Group, t.Key, t.Value })
               .IsUnique();

        Builder.HasIndex(t => t.UserId);
        Builder.HasIndex(t => t.PermissionId);
        Builder.HasIndex(t => t.Group);

        // Columns
        Builder.Property(t => t.UserId)
               .HasColumnName(nameof(UserPermissionCondition.UserId));

        Builder.Property(t => t.PermissionId)
               .HasColumnName(nameof(UserPermissionCondition.PermissionId));

        Builder.Property(t => t.Priority)
               .HasColumnName(nameof(UserPermissionCondition.Priority));

        Builder.Property(t => t.Group)
               .HasColumnName(nameof(UserPermissionCondition.Group))
               .HasMaxLength(50);

        Builder.Property(t => t.Key)
               .HasColumnName(nameof(UserPermissionCondition.Key))
               .HasMaxLength(50);

        Builder.Property(t => t.Value)
               .HasColumnName(nameof(UserPermissionCondition.Value))
               .HasMaxLength(200);

        Builder.Property(t => t.Allowed)
               .HasColumnName(nameof(UserPermissionCondition.Allowed));

        Builder.Property(t => t.Priority)
               .HasColumnName(nameof(UserPermissionCondition.Priority));

        // Relationships
        Builder.HasOne(t => t.User)
               .WithMany(t => t.PermissionConditions)
               .HasForeignKey(t => t.UserId);

        Builder.HasOne(t => t.Permission)
               .WithMany(t => t.UserPermissionConditions)
               .HasForeignKey(t => t.PermissionId);
    }
}
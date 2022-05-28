using Microsoft.EntityFrameworkCore;
using Netcorext.EntityFramework.UserIdentityPattern.Entities.Mapping;

namespace Netcorext.Auth.Domain.Entities.Mapping;

public class PermissionMap : EntityMap<Permission>
{
    public PermissionMap(ModelBuilder modelBuilder) : base(modelBuilder)
    {
        // Indexes
        Builder.HasIndex(t => new { t.Id, t.FunctionId, t.PermissionType, t.Allowed })
               .IsUnique();

        Builder.HasIndex(t => t.FunctionId);
        Builder.HasIndex(t => t.PermissionType);
        Builder.HasIndex(t => t.Allowed);
        Builder.HasIndex(t => t.ExpireDate);

        // Properties
        Builder.Property(t => t.FunctionId)
               .HasColumnName(nameof(Permission.FunctionId))
               .HasMaxLength(50);

        Builder.Property(t => t.PermissionType)
               .HasColumnName(nameof(Permission.PermissionType));

        Builder.Property(t => t.Allowed)
               .HasColumnName(nameof(Permission.Allowed));

        Builder.Property(t => t.Priority)
               .HasColumnName(nameof(Permission.Priority));
        
        Builder.Property(t => t.ReplaceExtendData)
               .HasColumnName(nameof(Permission.ReplaceExtendData));

        Builder.Property(t => t.ExpireDate)
               .HasColumnName(nameof(Permission.ExpireDate));

        // Relationships
        Builder.HasOne(t => t.Role)
               .WithMany(t => t.Permissions)
               .HasForeignKey(d => d.RoleId);
    }
}
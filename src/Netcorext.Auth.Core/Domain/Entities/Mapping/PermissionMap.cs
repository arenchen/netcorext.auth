using Microsoft.EntityFrameworkCore;
using Netcorext.EntityFramework.UserIdentityPattern.Entities.Mapping;

namespace Netcorext.Auth.Domain.Entities.Mapping;

public class PermissionMap : EntityMap<Permission>
{
    public PermissionMap(ModelBuilder modelBuilder) : base(modelBuilder)
    {
        // Indexes
        Builder.HasIndex(t => t.Name)
               .IsUnique();

        Builder.HasIndex(t => t.Disabled);

        // Properties
        Builder.Property(t => t.Name)
               .HasColumnName(nameof(Permission.Name))
               .HasMaxLength(50);

        Builder.Property(t => t.Priority)
               .HasColumnName(nameof(Permission.Priority));

        Builder.Property(t => t.Disabled)
               .HasColumnName(nameof(Permission.Disabled));
    }
}
using Microsoft.EntityFrameworkCore;
using Netcorext.EntityFramework.UserIdentityPattern.Entities.Mapping;

namespace Netcorext.Auth.Domain.Entities.Mapping;

public class RoleMap : EntityMap<Role>
{
    public RoleMap(ModelBuilder modelBuilder) : base(modelBuilder)
    {
        // Indexes
        Builder.HasIndex(t => t.Name);

        // Properties
        Builder.Property(t => t.Name)
               .HasColumnName(nameof(Role.Name))
               .HasMaxLength(50);
        
        Builder.Property(t => t.Priority)
               .HasColumnName(nameof(Role.Priority));

        Builder.Property(t => t.Disabled)
               .HasColumnName(nameof(Role.Disabled));
    }
}
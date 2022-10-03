using Microsoft.EntityFrameworkCore;
using Netcorext.EntityFramework.UserIdentityPattern.Entities.Mapping;

namespace Netcorext.Auth.Domain.Entities.Mapping;

public class RoleExtendDataMap : EntityMap<RoleExtendData>
{
    public RoleExtendDataMap(ModelBuilder modelBuilder) : base(modelBuilder)
    {
        // Key
        Builder.HasKey(t => new { t.Id, t.Key });

        // Indexes
        Builder.HasIndex(t => t.Key);
        Builder.HasIndex(t => t.Value);

        // Properties
        Builder.Property(t => t.Key)
               .HasColumnName(nameof(RoleExtendData.Key))
               .HasMaxLength(50);

        Builder.Property(t => t.Value)
               .HasColumnName(nameof(RoleExtendData.Value))
               .HasMaxLength(200);

        // Relationships
        Builder.HasOne(t => t.Role)
               .WithMany(t => t.ExtendData)
               .HasForeignKey(d => d.Id);
    }
}
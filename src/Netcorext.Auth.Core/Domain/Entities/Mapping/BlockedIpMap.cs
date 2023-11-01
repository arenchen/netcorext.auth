using Microsoft.EntityFrameworkCore;
using Netcorext.EntityFramework.UserIdentityPattern.Entities.Mapping;

namespace Netcorext.Auth.Domain.Entities.Mapping;

public class BlockedIpMap : EntityMap<BlockedIp>
{
    public BlockedIpMap(ModelBuilder modelBuilder) : base(modelBuilder)
    {
        // Indexes
        Builder.HasIndex(t => t.Cidr)
               .IsUnique();
        Builder.HasIndex(t => t.BeginRange);
        Builder.HasIndex(t => t.EndRange);
        Builder.HasIndex(t => t.Country);
        Builder.HasIndex(t => t.City);
        Builder.HasIndex(t => t.Asn);

        // Columns
        Builder.Property(t => t.Cidr)
               .HasColumnName(nameof(BlockedIp.Cidr))
               .HasMaxLength(50);

        Builder.Property(t => t.Country)
               .HasColumnName(nameof(BlockedIp.Country))
               .HasMaxLength(100);

        Builder.Property(t => t.City)
               .HasColumnName(nameof(BlockedIp.City))
               .HasMaxLength(100);

        Builder.Property(t => t.Asn)
               .HasColumnName(nameof(BlockedIp.Asn))
               .HasMaxLength(50);

        Builder.Property(t => t.Description)
               .HasColumnName(nameof(BlockedIp.Description))
               .HasMaxLength(200);

        // Relationships

    }
}

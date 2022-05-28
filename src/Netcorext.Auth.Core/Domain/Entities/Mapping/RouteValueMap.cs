using Microsoft.EntityFrameworkCore;
using Netcorext.EntityFramework.UserIdentityPattern.Entities.Mapping;

namespace Netcorext.Auth.Domain.Entities.Mapping;

public class RouteValueMap : EntityMap<RouteValue>
{
    public RouteValueMap(ModelBuilder modelBuilder) : base(modelBuilder)
    {
        // Key
        Builder.HasKey(t => new { t.Id, t.Key });

        // Indexes
        Builder.HasIndex(t => t.Key);
        Builder.HasIndex(t => t.Value);

        // Properties
        Builder.Property(t => t.Key)
               .HasColumnName(nameof(RouteValue.Key))
               .HasMaxLength(50);

        Builder.Property(t => t.Value)
               .HasColumnName(nameof(RouteValue.Value))
               .HasMaxLength(1000);

        // Relationships
        Builder.HasOne(t => t.Route)
               .WithMany(t => t.RouteValues)
               .HasForeignKey(d => d.Id)
               .Metadata.SetAnnotation("DeleteBehavior", DeleteBehavior.Cascade);
    }
}
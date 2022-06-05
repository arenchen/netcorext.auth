using Microsoft.EntityFrameworkCore;
using Netcorext.EntityFramework.UserIdentityPattern.Entities.Mapping;

namespace Netcorext.Auth.Domain.Entities.Mapping;

public class RouteGroupMap : EntityMap<RouteGroup>
{
    public RouteGroupMap(ModelBuilder modelBuilder) : base(modelBuilder)
    {
        // Indexes
        Builder.HasIndex(t => t.Name);

        // Columns
        Builder.Property(t => t.Name)
               .HasColumnName(nameof(RouteGroup.Name))
               .HasMaxLength(100);

        Builder.Property(t => t.BaseUrl)
               .HasColumnName(nameof(RouteGroup.BaseUrl))
               .HasMaxLength(200);


        Builder.Property(t => t.ForwarderRequestVersion)
               .HasColumnName(nameof(RouteGroup.ForwarderRequestVersion))
               .HasMaxLength(10);

        Builder.Property(t => t.ForwarderHttpVersionPolicy)
               .HasColumnName(nameof(RouteGroup.ForwarderHttpVersionPolicy));

        Builder.Property(t => t.ForwarderActivityTimeout)
               .HasColumnName(nameof(RouteGroup.ForwarderActivityTimeout));

        Builder.Property(t => t.ForwarderAllowResponseBuffering)
               .HasColumnName(nameof(RouteGroup.ForwarderAllowResponseBuffering));

        // Relationships
    }
}
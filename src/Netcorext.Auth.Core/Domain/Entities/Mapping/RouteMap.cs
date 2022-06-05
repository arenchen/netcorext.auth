using Microsoft.EntityFrameworkCore;
using Netcorext.EntityFramework.UserIdentityPattern.Entities.Mapping;

namespace Netcorext.Auth.Domain.Entities.Mapping;

public class RouteMap : EntityMap<Route>
{
    public RouteMap(ModelBuilder modelBuilder) : base(modelBuilder)
    {
        // Indexes
        Builder.HasIndex(t => new { t.HttpMethod, t.RelativePath })
               .IsUnique();

        Builder.HasIndex(t => t.Protocol);
        Builder.HasIndex(t => t.HttpMethod);
        Builder.HasIndex(t => t.RelativePath);
        Builder.HasIndex(t => t.Template);
        Builder.HasIndex(t => t.FunctionId);
        Builder.HasIndex(t => t.NativePermission);
        Builder.HasIndex(t => t.AllowAnonymous);
        Builder.HasIndex(t => t.Tag);

        // Properties
        Builder.Property(t => t.GroupId)
               .HasColumnName(nameof(Route.GroupId));

        Builder.Property(t => t.Protocol)
               .HasColumnName(nameof(Route.Protocol))
               .HasMaxLength(10);

        Builder.Property(t => t.HttpMethod)
               .HasColumnName(nameof(Route.HttpMethod))
               .HasMaxLength(10);

        Builder.Property(t => t.RelativePath)
               .HasColumnName(nameof(Route.RelativePath))
               .HasMaxLength(200);

        Builder.Property(t => t.Template)
               .HasColumnName(nameof(Route.Template))
               .HasMaxLength(200);

        Builder.Property(t => t.FunctionId)
               .HasColumnName(nameof(Route.FunctionId))
               .HasMaxLength(50);

        Builder.Property(t => t.NativePermission)
               .HasColumnName(nameof(Route.NativePermission));

        Builder.Property(t => t.AllowAnonymous)
               .HasColumnName(nameof(Route.AllowAnonymous));

        Builder.Property(t => t.Tag)
               .HasColumnName(nameof(Route.Tag))
               .HasMaxLength(200);

        // Relationships
        Builder.HasOne(t => t.Group)
               .WithMany(t => t.Routes)
               .HasForeignKey(d => d.GroupId);
    }
}
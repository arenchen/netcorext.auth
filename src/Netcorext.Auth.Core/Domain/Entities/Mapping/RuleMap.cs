using Microsoft.EntityFrameworkCore;
using Netcorext.EntityFramework.UserIdentityPattern.Entities.Mapping;

namespace Netcorext.Auth.Domain.Entities.Mapping;

public class RuleMap : EntityMap<Rule>
{
    public RuleMap(ModelBuilder modelBuilder) : base(modelBuilder)
    {
        // Indexes
        Builder.HasIndex(t => new { t.Id, t.FunctionId, t.PermissionType, t.Allowed })
               .IsUnique();

        Builder.HasIndex(t => t.FunctionId);
        Builder.HasIndex(t => t.PermissionType);
        Builder.HasIndex(t => t.Allowed);

        // Properties
        Builder.Property(t => t.FunctionId)
               .HasColumnName(nameof(Rule.FunctionId))
               .HasMaxLength(50);

        Builder.Property(t => t.PermissionType)
               .HasColumnName(nameof(Rule.PermissionType));

        Builder.Property(t => t.Allowed)
               .HasColumnName(nameof(Rule.Allowed));

        // Relationships
        Builder.HasOne(t => t.Permission)
               .WithMany(t => t.Rules)
               .HasForeignKey(d => d.PermissionId);
    }
}
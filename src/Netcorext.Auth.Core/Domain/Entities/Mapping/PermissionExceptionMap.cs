using Microsoft.EntityFrameworkCore;
using Netcorext.EntityFramework.UserIdentityPattern.Entities.Mapping;

namespace Netcorext.Auth.Domain.Entities.Mapping;

public class PermissionExceptionMap : EntityMap<PermissionExtendData>
{
    public PermissionExceptionMap(ModelBuilder modelBuilder) : base(modelBuilder)
    {
        Builder.HasKey(t => new { t.Id, t.Key, t.Value });

        // Columns
        Builder.Property(t => t.Key)
               .HasColumnName(nameof(PermissionExtendData.Key))
               .HasMaxLength(50);

        Builder.Property(t => t.Value)
               .HasColumnName(nameof(PermissionExtendData.Value))
               .HasMaxLength(200);

        Builder.Property(t => t.PermissionType)
               .HasColumnName(nameof(PermissionExtendData.PermissionType));

        Builder.Property(t => t.Allowed)
               .HasColumnName(nameof(PermissionExtendData.Allowed));

        // Relationships
        Builder.HasOne(t => t.Permission)
               .WithMany(t => t.ExtendData)
               .HasForeignKey(d => d.Id);
    }
}
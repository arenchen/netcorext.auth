using Microsoft.EntityFrameworkCore;
using Netcorext.EntityFramework.UserIdentityPattern.Entities.Mapping;

namespace Netcorext.Auth.Domain.Entities.Mapping;

public class UserExtendDataMap : EntityMap<UserExtendData>
{
    public UserExtendDataMap(ModelBuilder modelBuilder) : base(modelBuilder)
    {
        // Key
        Builder.HasKey(t => new { t.Id, t.Key });

        // Indexes
        Builder.HasIndex(t => t.Key);
        Builder.HasIndex(t => t.Value);

        // Properties
        Builder.Property(t => t.Key)
               .HasColumnName(nameof(UserExtendData.Key))
               .HasMaxLength(50);

        Builder.Property(t => t.Value)
               .HasColumnName(nameof(UserExtendData.Value))
               .HasMaxLength(1000);

        // Relationships
        Builder.HasOne(t => t.User)
               .WithMany(t => t.ExtendData)
               .HasForeignKey(d => d.Id);
    }
}
using Microsoft.EntityFrameworkCore;
using Netcorext.EntityFramework.UserIdentityPattern.Entities.Mapping;

namespace Netcorext.Auth.Domain.Entities.Mapping;

public class UserExternalLoginMap : EntityMap<UserExternalLogin>
{
    public UserExternalLoginMap(ModelBuilder modelBuilder) : base(modelBuilder)
    {
        // Key
        Builder.HasKey(t => new { t.Id, t.Provider });

        // Indexes
        Builder.HasIndex(t => t.Provider);
        Builder.HasIndex(t => t.UniqueId);


        // Properties
        Builder.Property(t => t.Provider)
               .HasColumnName(nameof(UserExternalLogin.Provider))
               .HasMaxLength(50);

        Builder.Property(t => t.UniqueId)
               .HasColumnName(nameof(UserExternalLogin.UniqueId))
               .HasMaxLength(100);

        // Relationships
        Builder.HasOne(t => t.User)
               .WithMany(t => t.ExternalLogins)
               .HasForeignKey(d => d.Id);
    }
}
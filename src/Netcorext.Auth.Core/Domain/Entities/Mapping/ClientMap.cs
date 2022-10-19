using Microsoft.EntityFrameworkCore;
using Netcorext.EntityFramework.UserIdentityPattern.Entities.Mapping;

namespace Netcorext.Auth.Domain.Entities.Mapping;

public class ClientMap : EntityMap<Client>
{
    public ClientMap(ModelBuilder modelBuilder) : base(modelBuilder)
    {
        // Indexes
        Builder.HasIndex(t => t.Name)
               .IsUnique();

        Builder.HasIndex(t => t.Disabled);

        // Properties
        Builder.Property(t => t.Secret)
               .HasColumnName(nameof(Client.Secret))
               .HasMaxLength(200);

        Builder.Property(t => t.Name)
               .HasColumnName(nameof(Client.Name))
               .HasMaxLength(50);

        Builder.Property(t => t.CallbackUrl)
               .HasColumnName(nameof(Client.CallbackUrl))
               .HasMaxLength(500);

        Builder.Property(t => t.AllowedRefreshToken)
               .HasColumnName(nameof(Client.AllowedRefreshToken));

        Builder.Property(t => t.TokenExpireSeconds)
               .HasColumnName(nameof(Client.TokenExpireSeconds));

        Builder.Property(t => t.RefreshTokenExpireSeconds)
               .HasColumnName(nameof(Client.RefreshTokenExpireSeconds));

        Builder.Property(t => t.CodeExpireSeconds)
               .HasColumnName(nameof(Client.CodeExpireSeconds));

        Builder.Property(t => t.Disabled)
               .HasColumnName(nameof(Client.Disabled));
    }
}
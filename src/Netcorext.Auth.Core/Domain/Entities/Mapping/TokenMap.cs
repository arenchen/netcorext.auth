using Microsoft.EntityFrameworkCore;
using Netcorext.EntityFramework.UserIdentityPattern.Entities.Mapping;

namespace Netcorext.Auth.Domain.Entities.Mapping;

public class TokenMap : EntityMap<Token>
{
    public TokenMap(ModelBuilder modelBuilder) : base(modelBuilder)
    {
        // Indexes
        Builder.HasIndex(t => t.ResourceType);
        Builder.HasIndex(t => t.ResourceId);
        Builder.HasIndex(t => t.TokenType);
        Builder.HasIndex(t => t.AccessToken);
        Builder.HasIndex(t => t.ExpiresIn);
        Builder.HasIndex(t => t.ExpiresAt);
        Builder.HasIndex(t => t.RefreshToken);
        Builder.HasIndex(t => t.Revoked);

        // Properties
        Builder.Property(t => t.ResourceType)
               .HasColumnName(nameof(Token.ResourceType))
               .HasMaxLength(50);

        Builder.Property(t => t.ResourceId)
               .HasColumnName(nameof(Token.ResourceId))
               .HasMaxLength(50);

        Builder.Property(t => t.TokenType)
               .HasColumnName(nameof(Token.TokenType))
               .HasMaxLength(50);

        Builder.Property(t => t.AccessToken)
               .HasColumnName(nameof(Token.AccessToken))
               .HasMaxLength(2048);

        Builder.Property(t => t.ExpiresIn)
               .HasColumnName(nameof(Token.ExpiresIn));

        Builder.Property(t => t.ExpiresAt)
               .HasColumnName(nameof(Token.ExpiresAt));

        Builder.Property(t => t.Scope)
               .HasColumnName(nameof(Token.Scope))
               .HasMaxLength(2048);

        Builder.Property(t => t.RefreshToken)
               .HasColumnName(nameof(Token.RefreshToken))
               .HasMaxLength(2048);

        Builder.Property(t => t.RefreshExpiresIn)
               .HasColumnName(nameof(Token.RefreshExpiresIn));

        Builder.Property(t => t.RefreshExpiresAt)
               .HasColumnName(nameof(Token.RefreshExpiresAt));

        Builder.Property(t => t.Revoked)
               .HasColumnName(nameof(Token.Revoked));
    }
}

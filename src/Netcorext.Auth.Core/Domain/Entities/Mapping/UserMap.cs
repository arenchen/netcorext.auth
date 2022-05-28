using Microsoft.EntityFrameworkCore;
using Netcorext.EntityFramework.UserIdentityPattern.Entities.Mapping;

namespace Netcorext.Auth.Domain.Entities.Mapping;

public class UserMap : EntityMap<User>
{
    public UserMap(ModelBuilder modelBuilder) : base(modelBuilder)
    {
        // Indexes
        Builder.HasIndex(t => t.Username)
               .IsUnique();

        Builder.HasIndex(t => t.NormalizedUsername)
               .IsUnique();

        Builder.HasIndex(t => t.Email);
        Builder.HasIndex(t => t.NormalizedEmail);
        Builder.HasIndex(t => t.PhoneNumber);
        Builder.HasIndex(t => t.Disabled);

        // Properties
        Builder.Property(t => t.Username)
               .HasColumnName(nameof(User.Username))
               .HasMaxLength(50);

        Builder.Property(t => t.NormalizedUsername)
               .HasColumnName(nameof(User.NormalizedUsername))
               .HasMaxLength(50);

        Builder.Property(t => t.Password)
               .HasColumnName(nameof(User.Password))
               .HasMaxLength(200);

        Builder.Property(t => t.Email)
               .HasColumnName(nameof(User.Email))
               .HasMaxLength(100);

        Builder.Property(t => t.NormalizedEmail)
               .HasColumnName(nameof(User.NormalizedEmail))
               .HasMaxLength(100);

        Builder.Property(t => t.EmailConfirmed)
               .HasColumnName(nameof(User.EmailConfirmed));

        Builder.Property(t => t.PhoneNumber)
               .HasColumnName(nameof(User.PhoneNumber))
               .HasMaxLength(50);

        Builder.Property(t => t.PhoneNumberConfirmed)
               .HasColumnName(nameof(User.PhoneNumberConfirmed));

        Builder.Property(t => t.Otp)
               .HasColumnName(nameof(User.Otp))
               .HasMaxLength(50);

        Builder.Property(t => t.OtpBound)
               .HasColumnName(nameof(User.OtpBound));

        Builder.Property(t => t.TwoFactorEnabled)
               .HasColumnName(nameof(User.TwoFactorEnabled));

        Builder.Property(t => t.RequiredChangePassword)
               .HasColumnName(nameof(User.RequiredChangePassword));

        Builder.Property(t => t.TokenExpireSeconds)
               .HasColumnName(nameof(User.TokenExpireSeconds));

        Builder.Property(t => t.RefreshTokenExpireSeconds)
               .HasColumnName(nameof(User.RefreshTokenExpireSeconds));

        Builder.Property(t => t.CodeExpireSeconds)
               .HasColumnName(nameof(User.CodeExpireSeconds));

        Builder.Property(t => t.AccessFailedCount)
               .HasColumnName(nameof(User.AccessFailedCount));

        Builder.Property(t => t.LastSignInDate)
               .HasColumnName(nameof(User.LastSignInDate));

        Builder.Property(t => t.LastSignInIp)
               .HasColumnName(nameof(User.LastSignInIp))
               .HasMaxLength(50);

        Builder.Property(t => t.Disabled)
               .HasColumnName(nameof(User.Disabled));
    }
}
using Lumen.Domain.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.DataAccess.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
               .ValueGeneratedNever();

        builder.Property(t => t.UserId)
               .IsRequired();

        builder.Property(t => t.TokenHash)
               .IsRequired()
               .HasMaxLength(128)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(t => t.CreatedByIp)
               .IsRequired()
               .HasMaxLength(64)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(t => t.ReplacedByTokenHash)
               .HasMaxLength(128)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(t => t.CreatedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(t => t.ExpiresAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(t => t.RevokedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(t => t.IsDeleted)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(t => t.DeletedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(t => t.TokenHash)
               .IsUnique()
               .HasDatabaseName("ix_refresh_tokens_token_hash");

        builder.HasIndex(t => t.UserId)
               .HasDatabaseName("ix_refresh_tokens_user_id");

        builder.HasIndex(t => t.ExpiresAt)
               .HasFilter("[IsDeleted] = 0")
               .HasDatabaseName("ix_refresh_tokens_expires_at");
    }
}

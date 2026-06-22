using Lumen.Domain.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.DataAccess.Persistence.Configurations;

internal sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
               .ValueGeneratedNever();

        builder.Property(t => t.UserId)
               .IsRequired();

        builder.Property(t => t.TokenHash)
               .IsRequired()
               .HasMaxLength(128)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(t => t.CreatedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(t => t.ExpiresAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(t => t.UsedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(t => t.IsDeleted)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(t => t.DeletedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(t => t.TokenHash)
               .IsUnique()
               .HasDatabaseName("ix_password_reset_tokens_token_hash");

        builder.HasIndex(t => t.UserId)
               .HasDatabaseName("ix_password_reset_tokens_user_id");
    }
}

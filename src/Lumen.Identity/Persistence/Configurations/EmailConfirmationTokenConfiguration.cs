using Lumen.Identity.Domain.Tokens;
using Lumen.SharedKernel.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.Identity.Persistence.Configurations;

internal sealed class EmailConfirmationTokenConfiguration : IEntityTypeConfiguration<EmailConfirmationToken>
{
    public void Configure(EntityTypeBuilder<EmailConfirmationToken> builder)
    {
        builder.ToTable("EmailConfirmationTokens", DatabaseSchemas.Identity);

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.UserId).IsRequired();

        builder.Property(t => t.TokenHash)
               .IsRequired()
               .HasMaxLength(128)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(t => t.UsedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(t => t.IsDeleted)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(t => t.DeletedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(t => t.TokenHash)
               .IsUnique()
               .HasDatabaseName("ix_identity_email_confirmation_tokens_hash");

        builder.HasIndex(t => t.UserId)
               .HasDatabaseName("ix_identity_email_confirmation_tokens_user_id");
    }
}

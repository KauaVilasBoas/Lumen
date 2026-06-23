using Lumen.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.DataAccess.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
               .ValueGeneratedNever();

        builder.Property(u => u.Email)
               .IsRequired()
               .HasMaxLength(256)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(u => u.Username)
               .IsRequired()
               .HasMaxLength(64)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(u => u.PasswordHash)
               .IsRequired()
               .HasMaxLength(128)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(u => u.IsBootstrap);

        builder.Property(u => u.IsActive)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(u => u.EmailConfirmedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(u => u.LastLoginAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(u => u.FailedLoginAttempts)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(u => u.LockedUntil)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(u => u.CreatedAt);

        builder.Property(u => u.UpdatedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(u => u.IsDeleted)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(u => u.DeletedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(u => u.Email)
               .IsUnique()
               .HasFilter("[IsDeleted] = 0")
               .HasDatabaseName("ix_users_email_unique");

        builder.HasIndex(u => u.Username)
               .IsUnique()
               .HasFilter("[IsDeleted] = 0")
               .HasDatabaseName("ix_users_username_unique");

        builder.HasIndex(u => u.LockedUntil)
               .HasFilter("[LockedUntil] IS NOT NULL")
               .HasDatabaseName("ix_users_locked_until");
    }
}

using Lumen.Modules.Identity.Domain.Authorization;
using Lumen.SharedKernel.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.Modules.Identity.Persistence.Configurations;

internal sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("Profiles", DatabaseSchemas.Identity);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name)
               .IsRequired()
               .HasMaxLength(128)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.Description)
               .IsRequired()
               .HasMaxLength(512)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.IsSystem)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.IsDeleted)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.DeletedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => p.Name)
               .IsUnique()
               .HasFilter("[IsDeleted] = 0")
               .HasDatabaseName("ix_identity_profiles_name_unique");
    }
}

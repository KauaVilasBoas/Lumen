using Lumen.Modules.Identity.Domain.Authorization;
using Lumen.SharedKernel.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.Modules.Identity.Persistence.Configurations;

internal sealed class PermissionProfileConfiguration : IEntityTypeConfiguration<PermissionProfile>
{
    public void Configure(EntityTypeBuilder<PermissionProfile> builder)
    {
        builder.ToTable("PermissionProfiles", DatabaseSchemas.Identity);

        builder.HasKey(pp => pp.Id);

        builder.Property(pp => pp.Id).ValueGeneratedNever();

        builder.Property(pp => pp.IsDeleted)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(pp => pp.DeletedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Permission>()
               .WithMany()
               .HasForeignKey(pp => pp.PermissionId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Profile>()
               .WithMany()
               .HasForeignKey(pp => pp.ProfileId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pp => new { pp.PermissionId, pp.ProfileId })
               .IsUnique()
               .HasFilter("[IsDeleted] = 0")
               .HasDatabaseName("ix_identity_permission_profiles_active_unique");

        builder.HasIndex(pp => pp.PermissionId)
               .HasDatabaseName("ix_identity_permission_profiles_permission_id");
    }
}

using Lumen.Modules.Identity.Domain.Authorization;
using Lumen.SharedKernel.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.Modules.Identity.Persistence.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions", DatabaseSchemas.Identity);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Code)
               .IsRequired()
               .HasMaxLength(256)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.Controller)
               .IsRequired()
               .HasMaxLength(128)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.Action)
               .IsRequired()
               .HasMaxLength(128)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.DisplayName)
               .IsRequired()
               .HasMaxLength(256)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.GroupPermissionId)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.IsOrphan)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.OrphanedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.IsDeleted)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.DeletedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => p.Code)
               .IsUnique()
               .HasFilter("[IsDeleted] = 0")
               .HasDatabaseName("ix_identity_permissions_code_unique");

        builder.HasOne<GroupPermission>()
               .WithMany()
               .HasForeignKey(p => p.GroupPermissionId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}

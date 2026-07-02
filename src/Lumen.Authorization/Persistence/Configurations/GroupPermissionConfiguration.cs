using Lumen.Authorization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.Authorization.Persistence.Configurations;

internal sealed class GroupPermissionConfiguration : IEntityTypeConfiguration<GroupPermission>
{
    public void Configure(EntityTypeBuilder<GroupPermission> builder)
    {
        builder.ToTable("PermissionGroup", LumenSchema.Name);

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id).ValueGeneratedNever();

        builder.Property(g => g.Name)
               .IsRequired()
               .HasMaxLength(128)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(g => g.Description)
               .IsRequired()
               .HasMaxLength(512)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(g => g.IsDeleted)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(g => g.DeletedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(g => g.Name)
               .IsUnique()
               .HasFilter("[IsDeleted] = 0")
               .HasDatabaseName("ix_lumen_permission_group_name_unique");
    }
}

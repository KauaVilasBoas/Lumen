using Lumen.Authorization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.Authorization.Persistence.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permission", LumenSchema.Name);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Code)
               .IsRequired()
               .HasMaxLength(256)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.DisplayName)
               .IsRequired()
               .HasMaxLength(256)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.GroupPermissionId)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.IsDeleted)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.DeletedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<GroupPermission>()
               .WithMany()
               .HasForeignKey(p => p.GroupPermissionId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}

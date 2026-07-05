using Lumen.Authorization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.Authorization.Persistence.Configurations;

internal sealed class PermissionProfileConfiguration : IEntityTypeConfiguration<PermissionProfile>
{
    public void Configure(EntityTypeBuilder<PermissionProfile> builder)
    {
        builder.ToTable("PermissionProfile", LumenSchema.Name);

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

        // Filtered unique index on (PermissionId, ProfileId) and lookup index on PermissionId are
        // registered in LumenAuthorizationDbContext.ApplyProviderAwareConfigurations with dialect-correct syntax.
    }
}

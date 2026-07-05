using Lumen.Authorization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.Authorization.Persistence.Configurations;

internal sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("Profile", LumenSchema.Name);

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

        // Filtered unique index on Name is registered in LumenAuthorizationDbContext.ApplyProviderAwareConfigurations
        // with dialect-correct syntax (SQL Server vs PostgreSQL).
    }
}

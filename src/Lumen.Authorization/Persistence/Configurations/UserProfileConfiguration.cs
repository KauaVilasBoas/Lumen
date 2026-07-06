using Lumen.Authorization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.Authorization.Persistence.Configurations;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfile", LumenSchema.Name);

        builder.HasKey(up => up.Id);

        builder.Property(up => up.Id).ValueGeneratedNever();

        builder.Property(up => up.ScopeId)
               .IsRequired(false);

        builder.Property(up => up.IsDeleted)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(up => up.DeletedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Profile>()
               .WithMany()
               .HasForeignKey(up => up.ProfileId)
               .OnDelete(DeleteBehavior.Restrict);

        // Filtered unique index on (UserId, ProfileId, ScopeId) and lookup index on UserId are
        // registered in LumenAuthorizationDbContext.ApplyProviderAwareConfigurations with
        // dialect-correct syntax (SQL Server vs PostgreSQL).
    }
}

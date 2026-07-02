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

        builder.Property(up => up.IsDeleted)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(up => up.DeletedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Profile>()
               .WithMany()
               .HasForeignKey(up => up.ProfileId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(up => new { up.UserId, up.ProfileId })
               .IsUnique()
               .HasFilter("[IsDeleted] = 0")
               .HasDatabaseName("ix_lumen_user_profile_active_unique");

        builder.HasIndex(up => up.UserId)
               .HasDatabaseName("ix_lumen_user_profile_user_id");
    }
}

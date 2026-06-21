using Lumen.Domain.Authorization;
using Lumen.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.DataAccess.Persistence.Configurations;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        builder.HasKey(up => up.Id);

        builder.Property(up => up.Id)
               .ValueGeneratedNever();

        builder.Property(up => up.UserId);
        builder.Property(up => up.ProfileId);

        builder.Property(up => up.IsDeleted)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(up => up.DeletedAt)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(up => up.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Authorization.Profile>()
               .WithMany()
               .HasForeignKey(up => up.ProfileId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(up => new { up.UserId, up.ProfileId })
               .IsUnique()
               .HasFilter("[IsDeleted] = 0")
               .HasDatabaseName("ix_user_profiles_active_unique");

        builder.HasIndex(up => up.UserId)
               .HasDatabaseName("ix_user_profiles_user_id");
    }
}

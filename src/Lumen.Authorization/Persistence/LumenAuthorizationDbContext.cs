using System.Linq.Expressions;
using Lumen.Authorization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lumen.Authorization.Persistence;

internal sealed class LumenAuthorizationDbContext : DbContext
{
    public LumenAuthorizationDbContext(DbContextOptions<LumenAuthorizationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<GroupPermission> GroupPermissions => Set<GroupPermission>();

    public DbSet<Profile> Profiles => Set<Profile>();

    public DbSet<PermissionProfile> PermissionProfiles => Set<PermissionProfile>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LumenAuthorizationDbContext).Assembly);
        ApplySoftDeleteFilters(modelBuilder);
    }

    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                continue;

            modelBuilder
                .Entity(entityType.ClrType)
                .HasQueryFilter(BuildIsNotDeletedFilter(entityType.ClrType));
        }
    }

    private static LambdaExpression BuildIsNotDeletedFilter(Type entityType)
    {
        ParameterExpression param = Expression.Parameter(entityType, "e");
        MemberExpression isDeleted = Expression.Property(param, nameof(ISoftDeletable.IsDeleted));
        UnaryExpression notDeleted = Expression.Not(isDeleted);
        return Expression.Lambda(notDeleted, param);
    }
}

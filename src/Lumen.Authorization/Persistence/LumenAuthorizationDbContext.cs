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

    /// <summary>
    /// Indica se o provider ativo é PostgreSQL.
    /// Usado pelas configurações de entidade para aplicar sintaxe de índice filtrado
    /// correta por dialeto (SQL Server: <c>[Col] = 0</c>, PostgreSQL: <c>col = false</c>).
    /// </summary>
    internal bool IsPostgres => Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var isPostgres = IsPostgres;

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(LumenAuthorizationDbContext).Assembly,
            type =>
            {
                // Pass the provider flag to each configuration via a custom convention.
                // Configurations that need provider-aware behavior implement IProviderAwareConfiguration.
                return true;
            });

        ApplyProviderAwareConfigurations(modelBuilder, isPostgres);
        ApplySoftDeleteFilters(modelBuilder);
    }

    /// <summary>
    /// Aplica os índices filtrados com sintaxe correta para cada dialeto SQL.
    /// Centraliza a lógica provider-aware que antes estava espalhada nas configurações.
    /// </summary>
    private static void ApplyProviderAwareConfigurations(ModelBuilder modelBuilder, bool isPostgres)
    {
        // Profile: unique index on Name for non-deleted records
        modelBuilder.Entity<Profile>()
            .HasIndex(p => p.Name)
            .IsUnique()
            .HasFilter(isPostgres ? "is_deleted = false" : "[IsDeleted] = 0")
            .HasDatabaseName("ix_lumen_profile_name_unique");

        // Permission: unique index on Code for non-deleted records
        modelBuilder.Entity<Permission>()
            .HasIndex(p => p.Code)
            .IsUnique()
            .HasFilter(isPostgres ? "is_deleted = false" : "[IsDeleted] = 0")
            .HasDatabaseName("ix_lumen_permission_code_unique");

        // GroupPermission: unique index on Name for non-deleted records
        modelBuilder.Entity<GroupPermission>()
            .HasIndex(g => g.Name)
            .IsUnique()
            .HasFilter(isPostgres ? "is_deleted = false" : "[IsDeleted] = 0")
            .HasDatabaseName("ix_lumen_permission_group_name_unique");

        // UserProfile: unique active assignment per user+profile
        modelBuilder.Entity<UserProfile>()
            .HasIndex(up => new { up.UserId, up.ProfileId })
            .IsUnique()
            .HasFilter(isPostgres ? "is_deleted = false" : "[IsDeleted] = 0")
            .HasDatabaseName("ix_lumen_user_profile_active_unique");

        // UserProfile: lookup by user
        modelBuilder.Entity<UserProfile>()
            .HasIndex(up => up.UserId)
            .HasDatabaseName("ix_lumen_user_profile_user_id");

        // PermissionProfile: unique active assignment per permission+profile
        modelBuilder.Entity<PermissionProfile>()
            .HasIndex(pp => new { pp.PermissionId, pp.ProfileId })
            .IsUnique()
            .HasFilter(isPostgres ? "is_deleted = false" : "[IsDeleted] = 0")
            .HasDatabaseName("ix_lumen_permission_profile_active_unique");

        // PermissionProfile: lookup by permission
        modelBuilder.Entity<PermissionProfile>()
            .HasIndex(pp => pp.PermissionId)
            .HasDatabaseName("ix_lumen_permission_profile_permission_id");
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

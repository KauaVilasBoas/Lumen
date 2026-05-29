using System.Linq.Expressions;
using AegisIdentity.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AegisIdentity.DataAccess.Persistence;

public sealed class AegisIdentityDbContext : DbContext
{
    public AegisIdentityDbContext(DbContextOptions<AegisIdentityDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AegisIdentityDbContext).Assembly);
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

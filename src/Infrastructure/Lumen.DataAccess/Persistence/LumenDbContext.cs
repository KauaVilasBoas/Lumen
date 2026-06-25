using System.Linq.Expressions;
using Lumen.Domain.Audit;
using Lumen.Domain.Authorization;
using Lumen.Domain.Common;
using Lumen.Domain.Tokens;
using Lumen.Domain.Users;
using Lumen.SharedKernel.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lumen.DataAccess.Persistence;

public sealed class LumenDbContext : DbContext
{
    private readonly IPublisher? _publisher;

    public LumenDbContext(DbContextOptions<LumenDbContext> options, IPublisher? publisher = null)
        : base(options)
    {
        _publisher = publisher;
    }

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<EmailConfirmationToken> EmailConfirmationTokens => Set<EmailConfirmationToken>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<Domain.Authorization.Profile> Profiles => Set<Domain.Authorization.Profile>();

    public DbSet<GroupPermission> GroupPermissions => Set<GroupPermission>();

    public DbSet<PermissionProfile> PermissionProfiles => Set<PermissionProfile>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var affectedRows = await base.SaveChangesAsync(cancellationToken);

        if (Database.CurrentTransaction is null)
            await DispatchDomainEventsAsync(cancellationToken);

        return affectedRows;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LumenDbContext).Assembly);
        ApplySoftDeleteFilters(modelBuilder);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        if (_publisher is null)
            return;

        var aggregates = ChangeTracker
            .Entries<AggregateRoot>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToList();

        var domainEvents = aggregates
            .SelectMany(aggregate => aggregate.DomainEvents)
            .ToList();

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);
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

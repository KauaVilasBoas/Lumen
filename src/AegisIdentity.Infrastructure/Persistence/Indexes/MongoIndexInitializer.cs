using AegisIdentity.Domain.Users;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AegisIdentity.Infrastructure.Persistence.Indexes;

/// <summary>
/// Hosted service that creates all required MongoDB indexes at application startup.
///
/// Running index creation in an <see cref="IHostedService"/> ensures indexes exist before
/// the app begins accepting requests, without blocking DI registration.
///
/// Idempotency: <c>CreateOneAsync</c> with a named index model is a no-op when the index
/// already exists with an identical definition, so restarts and rolling deploys are safe.
///
/// Indexes created:
/// <list type="bullet">
///   <item><c>email</c> — unique, enforces one account per address.</item>
///   <item><c>username</c> — unique, enforces distinct usernames (required in MVP).</item>
///   <item><c>lockedUntil</c> — sparse, supports efficient queries for auto-unlock jobs
///         without penalising the majority of documents where the field is absent.</item>
/// </list>
/// </summary>
public sealed class MongoIndexInitializer : IHostedService
{
    private readonly IMongoCollection<User> _users;
    private readonly ILogger<MongoIndexInitializer> _logger;

    public MongoIndexInitializer(
        MongoDbContext context,
        ILogger<MongoIndexInitializer> logger)
    {
        _users = context.GetCollection<User>(CollectionNames.Users);
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating MongoDB indexes for collection '{Collection}'",
            CollectionNames.Users);

        var indexModels = BuildIndexModels();

        foreach (var model in indexModels)
        {
            await _users.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
        }

        _logger.LogInformation("MongoDB index creation complete for '{Collection}'",
            CollectionNames.Users);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // ─── Index definitions ────────────────────────────────────────────────────

    private static IReadOnlyList<CreateIndexModel<User>> BuildIndexModels()
    {
        var emailIndex = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true, Name = "ix_email_unique" });

        var usernameIndex = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Username),
            new CreateIndexOptions { Unique = true, Name = "ix_username_unique" });

        // Sparse: only documents where lockedUntil exists are indexed.
        // This keeps the index small since most users are not locked at any given time.
        var lockedUntilIndex = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.LockedUntil),
            new CreateIndexOptions { Sparse = true, Name = "ix_lockedUntil_sparse" });

        return [emailIndex, usernameIndex, lockedUntilIndex];
    }
}

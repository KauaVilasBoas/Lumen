using AegisIdentity.Domain.Tokens;
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
///   <item><c>users.email</c> — unique, enforces one account per address.</item>
///   <item><c>users.username</c> — unique, enforces distinct usernames.</item>
///   <item><c>users.lockedUntil</c> — sparse, supports auto-unlock queries without
///         penalising the majority of documents where the field is absent.</item>
///   <item><c>refresh_tokens.tokenHash</c> — unique, token lookup by hash.</item>
///   <item><c>refresh_tokens.userId</c> — non-unique, list all tokens for a user.</item>
///   <item><c>refresh_tokens.expiresAt</c> — TTL, auto-deletes expired documents.</item>
///   <item><c>password_reset_tokens.tokenHash</c> — unique, token lookup by hash.</item>
///   <item><c>password_reset_tokens.expiresAt</c> — TTL, auto-deletes expired documents.</item>
///   <item><c>email_confirmation_tokens.tokenHash</c> — unique, token lookup by hash.</item>
///   <item><c>email_confirmation_tokens.expiresAt</c> — TTL, auto-deletes expired documents.</item>
/// </list>
/// </summary>
public sealed class MongoIndexInitializer : IHostedService
{
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<RefreshToken> _refreshTokens;
    private readonly IMongoCollection<PasswordResetToken> _passwordResetTokens;
    private readonly IMongoCollection<EmailConfirmationToken> _emailConfirmationTokens;
    private readonly ILogger<MongoIndexInitializer> _logger;

    public MongoIndexInitializer(
        MongoDbContext context,
        ILogger<MongoIndexInitializer> logger)
    {
        _users = context.GetCollection<User>(CollectionNames.Users);
        _refreshTokens = context.GetCollection<RefreshToken>(CollectionNames.RefreshTokens);
        _passwordResetTokens = context.GetCollection<PasswordResetToken>(CollectionNames.PasswordResetTokens);
        _emailConfirmationTokens = context.GetCollection<EmailConfirmationToken>(CollectionNames.EmailConfirmationTokens);
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await CreateUsersIndexesAsync(cancellationToken);
        await CreateRefreshTokenIndexesAsync(cancellationToken);
        await CreatePasswordResetTokenIndexesAsync(cancellationToken);
        await CreateEmailConfirmationTokenIndexesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // ─── Users ────────────────────────────────────────────────────────────────

    private async Task CreateUsersIndexesAsync(CancellationToken ct)
    {
        _logger.LogInformation("Creating MongoDB indexes for collection '{Collection}'", CollectionNames.Users);

        var models = new[]
        {
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true, Name = "ix_email_unique" }),

            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Username),
                new CreateIndexOptions { Unique = true, Name = "ix_username_unique" }),

            // Sparse: only documents where lockedUntil exists are indexed.
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.LockedUntil),
                new CreateIndexOptions { Sparse = true, Name = "ix_lockedUntil_sparse" }),
        };

        foreach (var model in models)
            await _users.Indexes.CreateOneAsync(model, cancellationToken: ct);

        _logger.LogInformation("MongoDB index creation complete for '{Collection}'", CollectionNames.Users);
    }

    // ─── RefreshTokens ────────────────────────────────────────────────────────

    private async Task CreateRefreshTokenIndexesAsync(CancellationToken ct)
    {
        _logger.LogInformation("Creating MongoDB indexes for collection '{Collection}'", CollectionNames.RefreshTokens);

        var models = new[]
        {
            // Unique hash lookup — primary access pattern.
            new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(t => t.TokenHash),
                new CreateIndexOptions { Unique = true, Name = "ix_tokenHash_unique" }),

            // Non-unique — list all tokens for a given user (session management).
            new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(t => t.UserId),
                new CreateIndexOptions { Name = "ix_userId" }),

            // TTL — MongoDB automatically deletes documents once ExpiresAt has passed.
            // ExpireAfter = TimeSpan.Zero means "delete as soon as ExpiresAt <= now".
            new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(t => t.ExpiresAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "ix_expiresAt_ttl" }),
        };

        foreach (var model in models)
            await _refreshTokens.Indexes.CreateOneAsync(model, cancellationToken: ct);

        _logger.LogInformation("MongoDB index creation complete for '{Collection}'", CollectionNames.RefreshTokens);
    }

    // ─── PasswordResetTokens ──────────────────────────────────────────────────

    private async Task CreatePasswordResetTokenIndexesAsync(CancellationToken ct)
    {
        _logger.LogInformation("Creating MongoDB indexes for collection '{Collection}'", CollectionNames.PasswordResetTokens);

        var models = new[]
        {
            new CreateIndexModel<PasswordResetToken>(
                Builders<PasswordResetToken>.IndexKeys.Ascending(t => t.TokenHash),
                new CreateIndexOptions { Unique = true, Name = "ix_tokenHash_unique" }),

            new CreateIndexModel<PasswordResetToken>(
                Builders<PasswordResetToken>.IndexKeys.Ascending(t => t.ExpiresAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "ix_expiresAt_ttl" }),
        };

        foreach (var model in models)
            await _passwordResetTokens.Indexes.CreateOneAsync(model, cancellationToken: ct);

        _logger.LogInformation("MongoDB index creation complete for '{Collection}'", CollectionNames.PasswordResetTokens);
    }

    // ─── EmailConfirmationTokens ──────────────────────────────────────────────

    private async Task CreateEmailConfirmationTokenIndexesAsync(CancellationToken ct)
    {
        _logger.LogInformation("Creating MongoDB indexes for collection '{Collection}'", CollectionNames.EmailConfirmationTokens);

        var models = new[]
        {
            new CreateIndexModel<EmailConfirmationToken>(
                Builders<EmailConfirmationToken>.IndexKeys.Ascending(t => t.TokenHash),
                new CreateIndexOptions { Unique = true, Name = "ix_tokenHash_unique" }),

            new CreateIndexModel<EmailConfirmationToken>(
                Builders<EmailConfirmationToken>.IndexKeys.Ascending(t => t.ExpiresAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "ix_expiresAt_ttl" }),
        };

        foreach (var model in models)
            await _emailConfirmationTokens.Indexes.CreateOneAsync(model, cancellationToken: ct);

        _logger.LogInformation("MongoDB index creation complete for '{Collection}'", CollectionNames.EmailConfirmationTokens);
    }
}

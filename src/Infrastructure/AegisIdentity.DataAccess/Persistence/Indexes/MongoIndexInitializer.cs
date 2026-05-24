using AegisIdentity.Domain.Tokens;
using AegisIdentity.Domain.Users;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AegisIdentity.DataAccess.Persistence.Indexes;

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

            // Sparse: LockedUntil is null for most users; full index would waste memory.
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.LockedUntil),
                new CreateIndexOptions { Sparse = true, Name = "ix_lockedUntil_sparse" }),
        };

        foreach (var model in models)
            await _users.Indexes.CreateOneAsync(model, cancellationToken: ct);

        _logger.LogInformation("MongoDB index creation complete for '{Collection}'", CollectionNames.Users);
    }

    private async Task CreateRefreshTokenIndexesAsync(CancellationToken ct)
    {
        _logger.LogInformation("Creating MongoDB indexes for collection '{Collection}'", CollectionNames.RefreshTokens);

        var models = new[]
        {
            new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(t => t.TokenHash),
                new CreateIndexOptions { Unique = true, Name = "ix_tokenHash_unique" }),

            new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(t => t.UserId),
                new CreateIndexOptions { Name = "ix_userId" }),

            // TTL with ExpireAfter = Zero: Mongo deletes documents once ExpiresAt <= now.
            new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(t => t.ExpiresAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "ix_expiresAt_ttl" }),
        };

        foreach (var model in models)
            await _refreshTokens.Indexes.CreateOneAsync(model, cancellationToken: ct);

        _logger.LogInformation("MongoDB index creation complete for '{Collection}'", CollectionNames.RefreshTokens);
    }

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

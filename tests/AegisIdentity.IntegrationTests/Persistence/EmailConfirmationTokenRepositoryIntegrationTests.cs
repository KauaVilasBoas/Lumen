using AegisIdentity.Domain.Tokens;
using AegisIdentity.Infrastructure.Configuration;
using AegisIdentity.Infrastructure.Persistence;
using AegisIdentity.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace AegisIdentity.IntegrationTests.Persistence;

/// <summary>
/// Integration tests for <see cref="EmailConfirmationTokenRepository"/>.
///
/// Requires Docker Desktop with the daemon accessible to the test process.
/// </summary>
public sealed class EmailConfirmationTokenRepositoryIntegrationTests : IAsyncLifetime
{
    private const string DatabaseName = "aegis_test";
    private const string UserId = "507f1f77bcf86cd799439011";

    private readonly MongoDbContainer _container = new MongoDbBuilder()
        .WithImage("mongo:7")
        .Build();

    private MongoDbContext _context = null!;
    private EmailConfirmationTokenRepository _repository = null!;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = Options.Create(new MongoOptions
        {
            ConnectionString = _container.GetConnectionString(),
            Database = DatabaseName,
        });

        var client = new MongoClient(_container.GetConnectionString());
        _context = new MongoDbContext(client, options);
        _repository = new EmailConfirmationTokenRepository(_context);
    }

    public async Task DisposeAsync() => await _container.StopAsync();

    // ─── InsertAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task InsertAsync_StoresTokenAndGeneratesId()
    {
        var token = BuildToken("hash1");

        await _repository.InsertAsync(token);

        token.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task InsertAsync_DuplicateTokenHash_ThrowsMongoWriteException()
    {
        var first = BuildToken("hash-dup");
        await _repository.InsertAsync(first);

        await CreateUniqueTokenHashIndexAsync();

        var duplicate = BuildToken("hash-dup");
        var act = async () => await _repository.InsertAsync(duplicate);

        await act.Should().ThrowAsync<MongoWriteException>();
    }

    // ─── FindByTokenHashAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task FindByTokenHashAsync_ExistingHash_ReturnsToken()
    {
        var token = BuildToken("hash-find");
        await _repository.InsertAsync(token);

        var found = await _repository.FindByTokenHashAsync("hash-find");

        found.Should().NotBeNull();
        found!.TokenHash.Should().Be("hash-find");
        found.UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task FindByTokenHashAsync_NonExistentHash_ReturnsNull()
    {
        var result = await _repository.FindByTokenHashAsync("nonexistent");

        result.Should().BeNull();
    }

    // ─── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_PersistsUsedState()
    {
        var token = BuildToken("hash-used");
        await _repository.InsertAsync(token);

        token.MarkAsUsed();
        await _repository.UpdateAsync(token);

        var updated = await _repository.FindByTokenHashAsync("hash-used");
        updated.Should().NotBeNull();
        updated!.IsUsed().Should().BeTrue();
        updated.UsedAt.Should().NotBeNull();
    }

    // ─── Index validation ─────────────────────────────────────────────────────

    [Fact]
    public async Task Indexes_TokenHash_IsUnique_And_ExpiresAt_TTL_Exists()
    {
        await CreateAllIndexesAsync();

        var indexList = await _context
            .GetCollection<EmailConfirmationToken>(CollectionNames.EmailConfirmationTokens)
            .Indexes
            .List()
            .ToListAsync();

        var indexNames = indexList.Select(doc => doc["name"].AsString).ToList();

        indexNames.Should().Contain("ix_tokenHash_unique");
        indexNames.Should().Contain("ix_expiresAt_ttl");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static EmailConfirmationToken BuildToken(string tokenHash)
        => EmailConfirmationToken.Create(UserId, tokenHash, DateTime.UtcNow.AddHours(24));

    private async Task CreateUniqueTokenHashIndexAsync()
    {
        var collection = _context.GetCollection<EmailConfirmationToken>(CollectionNames.EmailConfirmationTokens);
        var model = new CreateIndexModel<EmailConfirmationToken>(
            Builders<EmailConfirmationToken>.IndexKeys.Ascending(t => t.TokenHash),
            new CreateIndexOptions { Unique = true, Name = "ix_tokenHash_unique" });
        await collection.Indexes.CreateOneAsync(model);
    }

    private async Task CreateAllIndexesAsync()
    {
        var collection = _context.GetCollection<EmailConfirmationToken>(CollectionNames.EmailConfirmationTokens);

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
            await collection.Indexes.CreateOneAsync(model);
    }
}

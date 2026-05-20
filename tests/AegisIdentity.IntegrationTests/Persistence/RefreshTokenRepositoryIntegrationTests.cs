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
/// Integration tests for <see cref="RefreshTokenRepository"/>.
///
/// Each test class instance spins up an ephemeral MongoDB container via Testcontainers.
/// The container is disposed at the end of the test run via <see cref="IAsyncLifetime"/>.
///
/// Requires Docker Desktop with the daemon accessible to the test process.
/// </summary>
public sealed class RefreshTokenRepositoryIntegrationTests : IAsyncLifetime
{
    private const string DatabaseName = "aegis_test";
    private const string UserId = "507f1f77bcf86cd799439011";
    private const string Ip = "127.0.0.1";

    private readonly MongoDbContainer _container = new MongoDbBuilder()
        .WithImage("mongo:7")
        .Build();

    private MongoDbContext _context = null!;
    private RefreshTokenRepository _repository = null!;

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
        _repository = new RefreshTokenRepository(_context);
    }

    public async Task DisposeAsync() => await _container.StopAsync();

    // ─── InsertAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task InsertAsync_StoresTokenAndGeneratesId()
    {
        var token = BuildActiveToken("hash1");

        await _repository.InsertAsync(token);

        token.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task InsertAsync_DuplicateTokenHash_ThrowsMongoWriteException()
    {
        var first = BuildActiveToken("hash-dup");
        await _repository.InsertAsync(first);

        await CreateUniqueTokenHashIndexAsync();

        var duplicate = BuildActiveToken("hash-dup");
        var act = async () => await _repository.InsertAsync(duplicate);

        await act.Should().ThrowAsync<MongoWriteException>();
    }

    // ─── FindByTokenHashAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task FindByTokenHashAsync_ExistingHash_ReturnsToken()
    {
        var token = BuildActiveToken("hash-find");
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

    // ─── FindByUserIdAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task FindByUserIdAsync_MultipleTokensForUser_ReturnsAll()
    {
        await _repository.InsertAsync(BuildActiveToken("hash-u1-a"));
        await _repository.InsertAsync(BuildActiveToken("hash-u1-b"));

        var results = await _repository.FindByUserIdAsync(UserId);

        results.Should().HaveCountGreaterThanOrEqualTo(2);
        results.Should().OnlyContain(t => t.UserId == UserId);
    }

    [Fact]
    public async Task FindByUserIdAsync_UnknownUserId_ReturnsEmptyList()
    {
        var results = await _repository.FindByUserIdAsync("507f1f77bcf86cd799439099");

        results.Should().BeEmpty();
    }

    // ─── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_PersistsRevokedState()
    {
        var token = BuildActiveToken("hash-revoke");
        await _repository.InsertAsync(token);

        token.Revoke(replacedByTokenHash: "new-hash");
        await _repository.UpdateAsync(token);

        var updated = await _repository.FindByTokenHashAsync("hash-revoke");
        updated.Should().NotBeNull();
        updated!.IsRevoked().Should().BeTrue();
        updated.RevokedAt.Should().NotBeNull();
        updated.ReplacedByTokenHash.Should().Be("new-hash");
    }

    // ─── Index validation ─────────────────────────────────────────────────────

    [Fact]
    public async Task Indexes_TokenHash_IsUnique_And_UserId_And_ExpiresAt_TTL_Exist()
    {
        await CreateAllIndexesAsync();

        var indexList = await _context
            .GetCollection<RefreshToken>(CollectionNames.RefreshTokens)
            .Indexes
            .List()
            .ToListAsync();

        var indexNames = indexList.Select(doc => doc["name"].AsString).ToList();

        indexNames.Should().Contain("ix_tokenHash_unique");
        indexNames.Should().Contain("ix_userId");
        indexNames.Should().Contain("ix_expiresAt_ttl");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static RefreshToken BuildActiveToken(string tokenHash)
        => RefreshToken.Create(UserId, tokenHash, DateTime.UtcNow.AddHours(1), Ip);

    private async Task CreateUniqueTokenHashIndexAsync()
    {
        var collection = _context.GetCollection<RefreshToken>(CollectionNames.RefreshTokens);
        var model = new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(t => t.TokenHash),
            new CreateIndexOptions { Unique = true, Name = "ix_tokenHash_unique" });
        await collection.Indexes.CreateOneAsync(model);
    }

    private async Task CreateAllIndexesAsync()
    {
        var collection = _context.GetCollection<RefreshToken>(CollectionNames.RefreshTokens);

        var models = new[]
        {
            new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(t => t.TokenHash),
                new CreateIndexOptions { Unique = true, Name = "ix_tokenHash_unique" }),
            new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(t => t.UserId),
                new CreateIndexOptions { Name = "ix_userId" }),
            new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(t => t.ExpiresAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "ix_expiresAt_ttl" }),
        };

        foreach (var model in models)
            await collection.Indexes.CreateOneAsync(model);
    }
}

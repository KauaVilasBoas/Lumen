using AegisIdentity.Domain.Users;
using AegisIdentity.Infrastructure.Configuration;
using AegisIdentity.Infrastructure.Persistence;
using AegisIdentity.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace AegisIdentity.IntegrationTests.Persistence;

/// <summary>
/// Integration tests for <see cref="UserRepository"/> and the MongoDB index initializer.
///
/// Each test class instance spins up an ephemeral MongoDB container via Testcontainers.
/// The container is disposed at the end of the test run via <see cref="IAsyncLifetime"/>.
///
/// Requires Docker Desktop with the daemon accessible to the test process.
/// </summary>
public sealed class UserRepositoryIntegrationTests : IAsyncLifetime
{
    private const string DatabaseName = "aegis_test";

    private readonly MongoDbContainer _container = new MongoDbBuilder()
        .WithImage("mongo:7")
        .Build();

    private MongoDbContext _context = null!;
    private UserRepository _repository = null!;

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
        _repository = new UserRepository(_context);
    }

    public async Task DisposeAsync() => await _container.StopAsync();

    // ─── InsertAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task InsertAsync_StoresUserAndGeneratesId()
    {
        var user = User.Create("alice@example.com", "alice", "hash123");

        await _repository.InsertAsync(user);

        user.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task InsertAsync_DuplicateEmail_ThrowsMongoWriteException()
    {
        var first = User.Create("bob@example.com", "bob", "hash");
        var duplicate = User.Create("bob@example.com", "bobby", "hash");

        await _repository.InsertAsync(first);

        // Index enforcement happens only after MongoIndexInitializer runs.
        // For this test we create the index manually.
        await CreateUniqueEmailIndexAsync();

        var act = async () => await _repository.InsertAsync(duplicate);
        await act.Should().ThrowAsync<MongoWriteException>();
    }

    // ─── FindByEmailAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task FindByEmailAsync_ExistingEmail_ReturnsUser()
    {
        var user = User.Create("carol@example.com", "carol", "hash");
        await _repository.InsertAsync(user);

        var found = await _repository.FindByEmailAsync("carol@example.com");

        found.Should().NotBeNull();
        found!.Email.Should().Be("carol@example.com");
        found.Username.Should().Be("carol");
    }

    [Fact]
    public async Task FindByEmailAsync_NonExistentEmail_ReturnsNull()
    {
        var result = await _repository.FindByEmailAsync("nobody@example.com");

        result.Should().BeNull();
    }

    // ─── FindByIdAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task FindByIdAsync_ExistingId_ReturnsUser()
    {
        var user = User.Create("dave@example.com", "dave", "hash");
        await _repository.InsertAsync(user);

        var found = await _repository.FindByIdAsync(user.Id);

        found.Should().NotBeNull();
        found!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task FindByIdAsync_InvalidObjectIdFormat_ReturnsNull()
    {
        var result = await _repository.FindByIdAsync("not-a-valid-objectid");

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _repository.FindByIdAsync("000000000000000000000001");

        result.Should().BeNull();
    }

    // ─── FindByUsernameAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task FindByUsernameAsync_ExistingUsername_ReturnsUser()
    {
        var user = User.Create("eve@example.com", "eve", "hash");
        await _repository.InsertAsync(user);

        var found = await _repository.FindByUsernameAsync("eve");

        found.Should().NotBeNull();
        found!.Username.Should().Be("eve");
    }

    [Fact]
    public async Task FindByUsernameAsync_NonExistentUsername_ReturnsNull()
    {
        var result = await _repository.FindByUsernameAsync("ghost");

        result.Should().BeNull();
    }

    // ─── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_PersistsChangesToExistingDocument()
    {
        var user = User.Create("frank@example.com", "frank", "hash");
        await _repository.InsertAsync(user);

        user.IsActive = true;
        user.EmailConfirmedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(user);

        var updated = await _repository.FindByIdAsync(user.Id);
        updated.Should().NotBeNull();
        updated!.IsActive.Should().BeTrue();
        updated.EmailConfirmedAt.Should().NotBeNull();
    }

    // ─── Index validation ─────────────────────────────────────────────────────

    [Fact]
    public async Task Indexes_EmailAndUsername_AreUnique_AndLockedUntil_IsSparse()
    {
        await CreateAllIndexesAsync();

        var indexList = await _context
            .GetCollection<User>(CollectionNames.Users)
            .Indexes
            .List()
            .ToListAsync();

        var indexNames = indexList.Select(doc => doc["name"].AsString).ToList();

        indexNames.Should().Contain("ix_email_unique");
        indexNames.Should().Contain("ix_username_unique");
        indexNames.Should().Contain("ix_lockedUntil_sparse");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task CreateUniqueEmailIndexAsync()
    {
        var collection = _context.GetCollection<User>(CollectionNames.Users);
        var model = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true, Name = "ix_email_unique" });
        await collection.Indexes.CreateOneAsync(model);
    }

    private async Task CreateAllIndexesAsync()
    {
        var collection = _context.GetCollection<User>(CollectionNames.Users);

        var models = new[]
        {
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true, Name = "ix_email_unique" }),
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Username),
                new CreateIndexOptions { Unique = true, Name = "ix_username_unique" }),
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.LockedUntil),
                new CreateIndexOptions { Sparse = true, Name = "ix_lockedUntil_sparse" }),
        };

        foreach (var model in models)
            await collection.Indexes.CreateOneAsync(model);
    }
}

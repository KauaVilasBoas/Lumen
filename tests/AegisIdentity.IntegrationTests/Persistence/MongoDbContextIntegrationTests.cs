using AegisIdentity.DataAccess.HealthChecks;
using AegisIdentity.DataAccess.Persistence;
using AegisIdentity.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace AegisIdentity.IntegrationTests.Persistence;

public sealed class MongoDbContextIntegrationTests : IAsyncLifetime
{
    private const string DatabaseName = "aegis_test";

    private readonly MongoDbContainer _container = new MongoDbBuilder()
        .WithImage("mongo:7")
        .Build();

    private MongoDbContext _context = null!;

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
    }

    public async Task DisposeAsync() => await _container.StopAsync();

    [Fact]
    public void GetCollection_Returns_Valid_Collection_Handle()
    {
        var collection = _context.GetCollection<BsonDocument>("smoke");

        collection.Should().NotBeNull();
        collection.CollectionNamespace.CollectionName.Should().Be("smoke");
    }

    [Fact]
    public async Task GetCollection_Insert_And_Read_Document_Roundtrip()
    {
        var collection = _context.GetCollection<BsonDocument>("roundtrip");
        var document = new BsonDocument { { "key", "value" } };

        await collection.InsertOneAsync(document);

        var stored = await collection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();
        stored.Should().NotBeNull();
        stored["key"].AsString.Should().Be("value");
    }

    [Fact]
    public async Task HealthCheck_Returns_Healthy_When_Mongo_Is_Running()
    {
        var healthCheck = new MongoDbHealthCheck(_context);
        var registrationName = "mongodb";
        var registration = new HealthCheckRegistration(
            registrationName,
            _ => healthCheck,
            failureStatus: HealthStatus.Unhealthy,
            tags: null);
        var checkContext = new HealthCheckContext { Registration = registration };

        var result = await healthCheck.CheckHealthAsync(checkContext);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task HealthCheck_Returns_Unhealthy_When_Mongo_Is_Unreachable()
    {
        var unreachableClient = new MongoClient(
            new MongoClientSettings
            {
                Server = new MongoServerAddress("127.0.0.1", 27099),
                ServerSelectionTimeout = TimeSpan.FromMilliseconds(500),
                ConnectTimeout = TimeSpan.FromMilliseconds(500),
            });

        var unreachableOptions = Options.Create(new MongoOptions
        {
            ConnectionString = "mongodb://127.0.0.1:27099",
            Database = DatabaseName,
        });

        var unreachableContext = new MongoDbContext(unreachableClient, unreachableOptions);
        var healthCheck = new MongoDbHealthCheck(unreachableContext);
        var registration = new HealthCheckRegistration(
            "mongodb",
            _ => healthCheck,
            failureStatus: HealthStatus.Unhealthy,
            tags: null);
        var checkContext = new HealthCheckContext { Registration = registration };

        var result = await healthCheck.CheckHealthAsync(checkContext, CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().NotBeNull();
    }
}

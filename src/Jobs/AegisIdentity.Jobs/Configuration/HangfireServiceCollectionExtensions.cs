using AegisIdentity.Infrastructure.Configuration;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AegisIdentity.Jobs.Configuration;

public static class HangfireServiceCollectionExtensions
{
    /// <summary>
    /// Registers Hangfire with MongoDB as the backing store.
    ///
    /// Storage: a dedicated database named via "Hangfire:DatabaseName" in
    /// configuration (defaults to the app database name suffixed with
    /// "_hangfire").  Keeping Hangfire collections in a separate database
    /// simplifies independent backup and schema management without polluting
    /// the domain collections.
    ///
    /// The <see cref="IMongoClient"/> is constructed inline here so that it
    /// lives in the Hangfire configuration delegate — it must NOT be resolved
    /// from the DI container at this point because the delegate runs before
    /// all services are built.
    ///
    /// Migration strategy: <see cref="MigrateMongoMigrationStrategy"/> with
    /// <see cref="NoneMongoBackupStrategy"/> — runs schema migrations on
    /// startup without creating backup snapshots (suitable for dev/CI; swap
    /// to <see cref="CollectionMongoBackupStrategy"/> in production if needed).
    /// </summary>
    public static IServiceCollection AddAegisHangfire(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHangfire((serviceProvider, config) =>
        {
            var mongoOptions = serviceProvider
                .GetRequiredService<IOptions<MongoOptions>>()
                .Value;

            var hangfireDatabaseName = configuration["Hangfire:DatabaseName"]
                ?? $"{mongoOptions.Database}_hangfire";

            // Use the IMongoClient overload so Hangfire.Mongo uses the same
            // driver version (MongoDB.Driver 2.30.0) that the rest of the app
            // uses — avoids the MongoClientSettings type-mismatch that occurs
            // when calling the connection-string overload against an assembly
            // compiled against a different minor version of MongoDB.Driver.
            var mongoClient = new MongoClient(mongoOptions.ConnectionString);

            var mongoStorageOptions = new MongoStorageOptions
            {
                MigrationOptions = new MongoMigrationOptions
                {
                    MigrationStrategy = new MigrateMongoMigrationStrategy(),
                    BackupStrategy    = new NoneMongoBackupStrategy(),
                },
                CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.TailNotificationsCollection,
                CheckConnection         = true,
            };

            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseMongoStorage(mongoClient, hangfireDatabaseName, mongoStorageOptions);
        });

        return services;
    }

    /// <summary>
    /// Adds the Hangfire background server to the DI container so it starts
    /// with the host.  Call this only in the host responsible for running jobs
    /// (the Api in this setup).  The Backoffice only calls
    /// <see cref="AddAegisHangfire"/> so the dashboard can read from the same
    /// storage without starting a competing server.
    /// </summary>
    public static IServiceCollection AddAegisHangfireServer(
        this IServiceCollection services,
        Action<BackgroundJobServerOptions>? configure = null)
    {
        services.AddHangfireServer(options => configure?.Invoke(options));
        return services;
    }
}

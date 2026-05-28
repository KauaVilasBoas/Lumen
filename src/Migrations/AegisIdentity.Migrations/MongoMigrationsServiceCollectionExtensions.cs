using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.Migrations;

public static class MongoMigrationsServiceCollectionExtensions
{
    public static IServiceCollection AddMongoMigrations(this IServiceCollection services)
    {
        // Scoped so it composes with consumers that register IMongoDatabase as
        // Scoped (the Api does). For the CLI, both registrations end up living
        // for the lifetime of the single root scope — equivalent to Singleton.
        services.AddScoped<MongoMigrationHistoryRepository>();
        services.AddScoped<MongoMigrationRunner>();

        // Auto-discover every IMongoMigration in this assembly so adding a new
        // migration is a single-file change — no DI re-wiring required.
        var assembly = typeof(MongoMigrationsServiceCollectionExtensions).Assembly;
        var migrationTypes = assembly.GetTypes()
            .Where(t => typeof(IMongoMigration).IsAssignableFrom(t)
                        && t is { IsAbstract: false, IsInterface: false });

        foreach (var type in migrationTypes)
            services.AddTransient(typeof(IMongoMigration), type);

        return services;
    }

    public static IServiceCollection AddMongoMigrationsHostedService(this IServiceCollection services)
    {
        services.AddHostedService<MongoMigrationsHostedService>();
        return services;
    }
}

using AegisIdentity.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AegisIdentity.Infrastructure.Persistence;

/// <summary>
/// Extension methods for registering MongoDB infrastructure services into the DI container.
/// </summary>
public static class MongoDbServiceExtensions
{
    /// <summary>
    /// Registers <see cref="IMongoClient"/> as a singleton, <see cref="IMongoDatabase"/>
    /// as a scoped factory, and <see cref="MongoDbContext"/> as a singleton.
    ///
    /// Lifetime rationale:
    /// - <see cref="IMongoClient"/>: singleton — the driver manages its own connection pool;
    ///   one instance per process is the recommended practice.
    /// - <see cref="IMongoDatabase"/>: scoped — created cheaply per request from the singleton
    ///   client; aligns with unit-of-work boundaries and avoids thread-safety ambiguity.
    /// - <see cref="MongoDbContext"/>: singleton — wraps thread-safe driver handles and
    ///   owns the one-time convention registration; safe to reuse across requests.
    /// </summary>
    public static IServiceCollection AddMongoDb(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IMongoClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            return new MongoClient(options.ConnectionString);
        });

        services.AddScoped<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            var options = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            return client.GetDatabase(options.Database);
        });

        services.AddSingleton<MongoDbContext>();

        return services;
    }
}

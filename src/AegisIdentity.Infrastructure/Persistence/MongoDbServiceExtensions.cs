using AegisIdentity.Domain.Tokens;
using AegisIdentity.Domain.Users;
using AegisIdentity.Infrastructure.Configuration;
using AegisIdentity.Infrastructure.Persistence.Indexes;
using AegisIdentity.Infrastructure.Persistence.Repositories;
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
    /// Registers MongoDB core services, repositories, and the index initializer.
    ///
    /// Lifetime rationale:
    /// - <see cref="IMongoClient"/>: singleton — the driver manages its own connection pool;
    ///   one instance per process is the recommended practice.
    /// - <see cref="IMongoDatabase"/>: scoped — created cheaply per request from the singleton
    ///   client; aligns with unit-of-work boundaries and avoids thread-safety ambiguity.
    /// - <see cref="MongoDbContext"/>: singleton — wraps thread-safe driver handles and
    ///   owns the one-time convention and class-map registration; safe to reuse across requests.
    /// - <see cref="IUserRepository"/>, <see cref="IRefreshTokenRepository"/>,
    ///   <see cref="IPasswordResetTokenRepository"/>, <see cref="IEmailConfirmationTokenRepository"/>:
    ///   scoped — follow <see cref="IMongoDatabase"/> lifetime; each request scope gets a fresh
    ///   repository backed by the scoped database handle.
    /// - <see cref="MongoIndexInitializer"/>: hosted service — runs once on startup before
    ///   the app accepts requests.
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

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IEmailConfirmationTokenRepository, EmailConfirmationTokenRepository>();

        // Index initializer — runs on startup, idempotent on restarts.
        services.AddHostedService<MongoIndexInitializer>();

        return services;
    }
}

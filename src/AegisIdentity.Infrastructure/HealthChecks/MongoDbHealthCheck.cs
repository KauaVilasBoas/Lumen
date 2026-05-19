using AegisIdentity.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;

namespace AegisIdentity.Infrastructure.HealthChecks;

/// <summary>
/// Verifies MongoDB connectivity by issuing a lightweight <c>{ ping: 1 }</c> command
/// against the configured database.  Registered under the name "mongodb" so that
/// dedicated health endpoints can filter by name.
/// </summary>
public sealed class MongoDbHealthCheck : IHealthCheck
{
    private readonly MongoDbContext _context;

    public MongoDbHealthCheck(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1),
                cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy("MongoDB is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "MongoDB ping failed.",
                exception: ex);
        }
    }
}

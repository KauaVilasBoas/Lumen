using AegisIdentity.Domain.Tokens;
using AegisIdentity.Jobs.Contracts;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.Jobs.Jobs;

/// <summary>
/// Recurring Hangfire job that bulk-deletes refresh tokens whose
/// <see cref="RefreshToken.ExpiresAt"/> has already passed.
///
/// Scheduling: auto-registered via <see cref="IJobDefinition"/> — no manual
/// wiring in Program.cs is required.  The cron expression "0 3 * * *" runs
/// the cleanup daily at 03:00 UTC, a low-traffic window.
///
/// Why: expired tokens are never reusable (the login flow always checks
/// <see cref="RefreshToken.IsActive()"/> before accepting a token), so
/// retaining them indefinitely wastes storage and makes user-scoped queries
/// slower.  Revoking a token is an explicit operation; this job only
/// touches tokens that are already past their <see cref="RefreshToken.ExpiresAt"/>.
/// </summary>
public sealed class CleanupExpiredRefreshTokensJob : IJobDefinition
{
    private readonly IRefreshTokenRepository _repository;
    private readonly ILogger<CleanupExpiredRefreshTokensJob> _logger;

    public string Name => "cleanup-expired-refresh-tokens";
    public string Cron => "0 3 * * *";

    public CleanupExpiredRefreshTokensJob(
        IRefreshTokenRepository repository,
        ILogger<CleanupExpiredRefreshTokensJob> logger)
    {
        _repository = repository;
        _logger     = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow;

        _logger.LogInformation(
            "Starting cleanup of expired refresh tokens (cutoff: {Cutoff:O})", cutoff);

        var deleted = await _repository.DeleteExpiredAsync(cutoff, cancellationToken);

        _logger.LogInformation(
            "Finished cleanup of expired refresh tokens — {Count} document(s) deleted", deleted);
    }
}

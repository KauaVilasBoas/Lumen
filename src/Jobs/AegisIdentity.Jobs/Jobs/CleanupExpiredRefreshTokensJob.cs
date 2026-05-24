using AegisIdentity.Domain.Tokens;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.Jobs.Jobs;

/// <summary>
/// Recurring Hangfire job that bulk-deletes refresh tokens whose
/// <see cref="RefreshToken.ExpiresAt"/> has already passed.
///
/// Scheduling: registered in the Api startup via
/// <c>RecurringJob.AddOrUpdate&lt;CleanupExpiredRefreshTokensJob&gt;(...)</c>
/// using the cron expression "0 3 * * *" (daily at 03:00 UTC).
///
/// Why: expired tokens are never reusable (the login flow always checks
/// <see cref="RefreshToken.IsActive()"/> before accepting a token), so
/// retaining them indefinitely wastes storage and makes user-scoped queries
/// slower.  Revoking a token is an explicit operation; this job only
/// touches tokens that are already past their <see cref="RefreshToken.ExpiresAt"/>.
/// </summary>
public sealed class CleanupExpiredRefreshTokensJob
{
    private readonly IRefreshTokenRepository _repository;
    private readonly ILogger<CleanupExpiredRefreshTokensJob> _logger;

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

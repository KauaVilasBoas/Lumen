using AegisIdentity.Domain.Tokens;
using AegisIdentity.Jobs.Contracts;
using AegisIdentity.SharedKernel.Constants;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.Jobs.Jobs;

public sealed class CleanupExpiredRefreshTokensJob : IJobDefinition
{
    private readonly IRefreshTokenRepository _repository;
    private readonly ILogger<CleanupExpiredRefreshTokensJob> _logger;

    public string Name => "cleanup-expired-refresh-tokens";
    public string Cron => JobSchedules.DailyAt3Am;

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

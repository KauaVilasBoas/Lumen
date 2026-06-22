using Lumen.Domain.Audit;
using Lumen.Domain.Tokens;
using Lumen.Jobs.Contracts;
using Lumen.SharedKernel.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lumen.Jobs.Jobs;

public sealed class CleanupExpiredRefreshTokensJob : IJobDefinition
{
    private readonly IRefreshTokenRepository _repository;
    private readonly IPublisher _publisher;
    private readonly ILogger<CleanupExpiredRefreshTokensJob> _logger;

    public string Name => "cleanup-expired-refresh-tokens";
    public string Cron => JobSchedules.DailyAt3Am;

    public CleanupExpiredRefreshTokensJob(
        IRefreshTokenRepository repository,
        IPublisher publisher,
        ILogger<CleanupExpiredRefreshTokensJob> logger)
    {
        _repository = repository;
        _publisher  = publisher;
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

        await _publisher.Publish(new CleanupJobExecuted(JobSchedules.CleanupJobName, deleted), cancellationToken);
    }
}

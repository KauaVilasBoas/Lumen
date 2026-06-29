using Lumen.Jobs.Contracts;
using Lumen.Modularity;
using Lumen.Modules.Audit.Contracts.Events;
using Lumen.Modules.Identity.Application.Tokens;
using Lumen.SharedKernel.Constants;
using Microsoft.Extensions.Logging;

namespace Lumen.Jobs.Jobs;

public sealed class CleanupExpiredRefreshTokensJob : IJobDefinition
{
    private readonly ITokenCleanupService _tokenCleanupService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CleanupExpiredRefreshTokensJob> _logger;

    public string Name => "cleanup-expired-refresh-tokens";
    public string Cron => JobSchedules.DailyAt3Am;

    public CleanupExpiredRefreshTokensJob(
        ITokenCleanupService tokenCleanupService,
        IEventBus eventBus,
        ILogger<CleanupExpiredRefreshTokensJob> logger)
    {
        _tokenCleanupService = tokenCleanupService;
        _eventBus  = eventBus;
        _logger    = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow;

        _logger.LogInformation(
            "Starting cleanup of expired refresh tokens (cutoff: {Cutoff:O})", cutoff);

        var deleted = await _tokenCleanupService.DeleteExpiredRefreshTokensAsync(cutoff, cancellationToken);

        _logger.LogInformation(
            "Finished cleanup of expired refresh tokens — {Count} document(s) deleted", deleted);

        await _eventBus.PublishAsync(
            new CleanupJobExecutedEvent(JobSchedules.CleanupJobName, deleted),
            cancellationToken);
    }
}

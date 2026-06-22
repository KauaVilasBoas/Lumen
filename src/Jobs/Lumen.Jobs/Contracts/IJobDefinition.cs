namespace Lumen.Jobs.Contracts;

/// <summary>
/// Defines a self-describing recurring Hangfire job.
///
/// Implement this interface on every recurring job class.  The auto-registration
/// mechanism in <see cref="Lumen.Jobs.Configuration.HangfireServiceCollectionExtensions"/>
/// discovers all concrete implementations via reflection and registers them in
/// the DI container.  The scheduler extension in
/// <see cref="Lumen.Jobs.Scheduling.HangfireSchedulerExtensions"/>
/// resolves every registered <see cref="IJobDefinition"/> and calls
/// <c>RecurringJob.AddOrUpdate</c> automatically — no manual wiring in
/// Program.cs is needed when a new job is added.
/// </summary>
public interface IJobDefinition
{
    /// <summary>Stable recurring-job identifier used by Hangfire (e.g. "cleanup-expired-refresh-tokens").</summary>
    string Name { get; }

    /// <summary>Cron expression for the schedule (e.g. "0 3 * * *" for daily at 03:00 UTC).</summary>
    string Cron { get; }

    /// <summary>Executes the job body.</summary>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

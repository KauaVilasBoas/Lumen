using AegisIdentity.Jobs.Contracts;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.Jobs.Scheduling;

/// <summary>
/// Extension method that resolves every registered <see cref="IJobDefinition"/>
/// from the DI container and schedules each one as a Hangfire recurring job.
///
/// Call this on the built <see cref="WebApplication"/> (after
/// <c>builder.Build()</c>) so the DI container is fully constructed:
/// <code>
/// var app = builder.Build();
/// app.ScheduleRecurringJobs();
/// </code>
///
/// Why post-Build: <c>RecurringJob.AddOrUpdate</c> requires Hangfire storage to
/// be initialised, which happens when the <see cref="IBackgroundJobClient"/>
/// infrastructure is built — i.e. after the container is resolved.
/// </summary>
public static class HangfireSchedulerExtensions
{
    /// <summary>
    /// Resolves all <see cref="IJobDefinition"/> implementations from a
    /// transient DI scope and registers them as Hangfire recurring jobs.
    ///
    /// Each job is identified by <see cref="IJobDefinition.Name"/> and
    /// scheduled with <see cref="IJobDefinition.Cron"/>.  An existing recurring
    /// job with the same name is updated in-place (idempotent on restart).
    /// </summary>
    public static WebApplication ScheduleRecurringJobs(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        // Resolve the service-based IRecurringJobManager from DI rather than using
        // the static RecurringJob.AddOrUpdate. Resolving it initialises the
        // DI-registered JobStorage (and JobStorage.Current) — the static API throws
        // "Current JobStorage instance has not been initialized yet" because nothing
        // has resolved Hangfire storage at this point in startup (the server hosted
        // service only starts later, during app.Run()).
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        var jobs = scope.ServiceProvider.GetServices<IJobDefinition>();

        foreach (var job in jobs)
        {
            var capturedJob = job;

            recurringJobManager.AddOrUpdate(
                recurringJobId: capturedJob.Name,
                methodCall:     () => capturedJob.ExecuteAsync(CancellationToken.None),
                cronExpression: capturedJob.Cron);
        }

        return app;
    }
}

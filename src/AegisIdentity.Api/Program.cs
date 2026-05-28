using AegisIdentity.Api.ExceptionHandlers;
using AegisIdentity.Api.Middleware;
using AegisIdentity.CommandHandlers.Auth.Register;
using AegisIdentity.CommandHandlers.Behaviors;
using AegisIdentity.DataAccess.HealthChecks;
using AegisIdentity.DataAccess.Persistence;
using AegisIdentity.Infrastructure.Configuration;
using AegisIdentity.Infrastructure.Security;
using AegisIdentity.Integration.Notifications;
using AegisIdentity.Integration.Security;
using AegisIdentity.Jobs.Configuration;
using AegisIdentity.Jobs.Scheduling;
using AegisIdentity.Migrations;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting AegisIdentity API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext());

    // ── Infrastructure ────────────────────────────────────────────────────────
    builder.Services.AddInfrastructureOptions(builder.Configuration);
    builder.Services.AddMongoDb(builder.Configuration);
    builder.Services.AddSecurity();
    builder.Services.AddHibpClient();
    builder.Services.AddNotifications();

    // ── Database migrations (Mongo) ──────────────────────────────────────────
    // Replaces the old MongoIndexInitializer: indexes (and any future schema
    // tweaks) are now versioned migrations under AegisIdentity.Migrations and
    // applied automatically on startup via MongoMigrationsHostedService.
    builder.Services.AddMongoMigrations();
    builder.Services.AddMongoMigrationsHostedService();

    // ── Background Jobs (Hangfire + Mongo storage) ───────────────────────────
    // AddInfrastructureOptions is called earlier — MongoOptions is already
    // registered in the DI container and available to AddAegisHangfire.
    // RegisterJobs scans AegisIdentity.Jobs for IJobDefinition implementations
    // and registers them in DI — no manual per-job wiring needed.
    builder.Services.AddAegisHangfire(builder.Configuration);
    builder.Services.AddAegisHangfireServer();
    builder.Services.RegisterJobs();

    // ── Application (MediatR + FluentValidation) ──────────────────────────────
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<RegisterUserCommandHandler>();
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    });

    builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserCommandHandler>();

    // ── Presentation ──────────────────────────────────────────────────────────
    builder.Services.AddControllers();

    builder.Services.AddExceptionHandler<BusinessExceptionHandler>();
    builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
    builder.Services.AddProblemDetails();

    // ── Health checks ─────────────────────────────────────────────────────────
    builder.Services
        .AddHealthChecks()
        .AddCheck<MongoDbHealthCheck>("mongodb");

    builder.Services.AddRazorPages();

    var app = builder.Build();

    // ── Recurring jobs ────────────────────────────────────────────────────────
    // ScheduleRecurringJobs resolves all IJobDefinition implementations from DI
    // and registers each via RecurringJob.AddOrUpdate — idempotent on restart.
    // To add a new job: implement IJobDefinition.  No changes here required.
    app.ScheduleRecurringJobs();

    // Reject loopback SMTP in Production — would silently discard all outbound emails.
    if (app.Environment.IsProduction())
    {
        var smtpOptions = app.Services.GetRequiredService<IOptions<SmtpOptions>>().Value;
        var localhostAliases = new[] { "localhost", "127.0.0.1", "::1" };

        if (localhostAliases.Contains(smtpOptions.Host, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Smtp:Host is set to '{smtpOptions.Host}' which resolves to localhost. " +
                "Configuring a loopback SMTP relay in Production will silently discard all outbound emails. " +
                "Set Smtp:Host to a real SMTP server (e.g. smtp.sendgrid.net) before deploying.");
        }
    }

    // ── Middleware pipeline ───────────────────────────────────────────────────

    // Global exception handler must be first so it catches exceptions from all middleware.
    app.UseExceptionHandler();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    // Must run before UseSerilogRequestLogging so the request-completion entry carries CorrelationId.
    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSerilogRequestLogging(opts =>
    {
        opts.GetLevel = (httpContext, _, _) =>
            httpContext.Request.Path.StartsWithSegments("/health")
                ? LogEventLevel.Verbose
                : LogEventLevel.Information;
    });

    // ── Endpoints ─────────────────────────────────────────────────────────────
    app.MapRazorPages();

    app.MapHealthChecks("/health/db", new HealthCheckOptions
    {
        Predicate = registration => registration.Name == "mongodb",
    });

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AegisIdentity API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

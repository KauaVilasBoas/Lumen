using Lumen.Api.Authorization;
using Lumen.Api.ExceptionHandlers;
using Lumen.Api.Hubs;
using Lumen.Api.Middleware;
using Lumen.CommandHandlers.Auth.Register;
using Lumen.EventHandlers.Authorization;
using Lumen.ReadModels.Queries;
using Lumen.CommandHandlers.Behaviors;
using Lumen.DataAccess.Cache;
using Lumen.DataAccess.HealthChecks;
using Lumen.DataAccess.Persistence;
using Lumen.Infrastructure.Configuration;
using Lumen.Infrastructure.Security;
using Lumen.Integration.Notifications;
using Lumen.Integration.Security;
using Lumen.Jobs.Configuration;
using Lumen.Jobs.Scheduling;
using Lumen.Migrations;
using Lumen.SharedKernel.Constants;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Lumen API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext());

    // ── Infrastructure ────────────────────────────────────────────────────────
    builder.Services.AddInfrastructureOptions(builder.Configuration, builder.Environment.IsProduction());
    builder.Services.AddRelationalDataAccess();
    builder.Services.AddDomainServices();
    builder.Services.AddRedisCache(builder.Configuration);
    builder.Services.AddSecurity();
    builder.Services.AddHibpClient();
    builder.Services.AddNotifications();

    // ── EF Core migrations applied on startup ────────────────────────────────
    // EfMigrationsHostedService runs Database.Migrate() before Hangfire starts
    // processing jobs, guaranteeing the schema is current before any job reads it.
    // PermissionDiscoveryHostedService is registered immediately after so that
    // IHostedService execution order guarantees migrations run before discovery.
    builder.Services.AddEfMigrationsHostedService();
    builder.Services.AddPermissionDiscovery();
    builder.Services.AddPermissionEnforcement();

    // ── Background Jobs (Hangfire + SQL Server storage) ──────────────────────
    // RegisterJobs scans Lumen.Jobs for IJobDefinition implementations
    // and registers them in DI — no manual per-job wiring needed.
    builder.Services.AddAegisHangfire(builder.Configuration);
    builder.Services.AddAegisHangfireServer();
    builder.Services.RegisterJobs();

    // ── Application (MediatR + FluentValidation) ──────────────────────────────
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<RegisterUserCommandHandler>();
        cfg.RegisterServicesFromAssemblyContaining<GetCurrentUserQueryHandler>();
        cfg.RegisterServicesFromAssemblyContaining<UserPermissionsChangedHandler>();
        cfg.RegisterServicesFromAssemblyContaining<GraphLivePushHandler>();
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    });

    builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserCommandHandler>();
    builder.Services.AddValidatorsFromAssemblyContaining<GetCurrentUserQueryHandler>();

    // ── Presentation ──────────────────────────────────────────────────────────
    builder.Services.AddSignalR();
    builder.Services.AddControllers();

    builder.Services.AddExceptionHandler<BusinessExceptionHandler>();
    builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
    builder.Services.AddProblemDetails();

    // ── Health checks ─────────────────────────────────────────────────────────
    builder.Services
        .AddHealthChecks()
        .AddCheck<SqlServerHealthCheck>("sqlserver")
        .AddRedisHealthCheck();

    builder.Services.AddRazorPages();

    var app = builder.Build();

    // ── Recurring jobs ────────────────────────────────────────────────────────
    // ScheduleRecurringJobs resolves all IJobDefinition implementations from DI
    // and registers each via RecurringJob.AddOrUpdate — idempotent on restart.
    // To add a new job: implement IJobDefinition.  No changes here required.
    if (!app.Environment.IsEnvironment("Testing"))
        app.ScheduleRecurringJobs();

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

    // Authentication and authorization must be placed after UseRouting and before
    // MapControllers so the middleware can short-circuit unauthenticated requests
    // before the endpoint handler is reached.
    app.UseAuthentication();
    app.UseAuthorization();

    // ── Endpoints ─────────────────────────────────────────────────────────────
    app.MapRazorPages();

    app.MapHealthChecks("/health/db", new HealthCheckOptions
    {
        Predicate = registration => registration.Name == "sqlserver",
    }).AllowAnonymous();

    app.MapHealthChecks("/health/cache", new HealthCheckOptions
    {
        Predicate = registration => registration.Name == "redis",
    }).AllowAnonymous();

    app.MapControllers();
    app.MapHub<AuthorizationGraphHub>(HubRoutes.AuthorizationGraph);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Lumen API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }

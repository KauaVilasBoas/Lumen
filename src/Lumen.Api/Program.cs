using Lumen.Api.Authorization;
using Lumen.Api.ExceptionHandlers;
using Lumen.Api.Hubs;
using Lumen.Api.Middleware;
using Lumen.Infrastructure.Configuration;
using Lumen.Jobs.Configuration;
using Lumen.Jobs.Scheduling;
using Lumen.Modularity;
using Lumen.Modules.Audit;
using Lumen.Modules.Audit.Migrations;
using Lumen.Modules.Identity;
using Lumen.Modules.Identity.Infrastructure.Security;
using Lumen.Modules.Identity.Migrations;
using Lumen.SharedKernel.Constants;
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

    // ── SQL Server options for Hangfire storage ───────────────────────────────
    builder.Services.AddSqlServerOptions(builder.Configuration);

    // ── Modules (auto-discovery via [Module] annotation) ─────────────────────
    builder.Services.AddModules(builder.Configuration,
        typeof(AuditModule).Assembly,
        typeof(IdentityModule).Assembly);

    // ── Event bus — includes host-level handlers (GraphLivePushHandler) ──────
    builder.Services.AddEventBus(
        typeof(AuditModule).Assembly,
        typeof(IdentityModule).Assembly,
        typeof(Program).Assembly);

    // ── Module migrations applied on startup ─────────────────────────────────
    builder.Services.AddAuditMigrationsHostedService();
    builder.Services.AddIdentityMigrationsHostedService();

    // ── Authentication & Authorization ────────────────────────────────────────
    builder.Services.AddIdentityJwtBearerAuthentication(HubRoutes.AuthorizationGraph);

    // ── Permission discovery and Administrator reconciliation ────────────────
    builder.Services.AddPermissionDiscovery();
    builder.Services.AddPermissionEnforcement();

    // ── Background Jobs (Hangfire + SQL Server storage) ──────────────────────
    builder.Services.AddAegisHangfire(builder.Configuration);
    builder.Services.AddAegisHangfireServer();
    builder.Services.RegisterJobs();

    // ── Presentation ──────────────────────────────────────────────────────────
    builder.Services.AddSignalR();
    builder.Services.AddControllers();

    builder.Services.AddExceptionHandler<BusinessExceptionHandler>();
    builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
    builder.Services.AddProblemDetails();

    // ── Health checks ─────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks();

    builder.Services.AddRazorPages();

    var app = builder.Build();

    // ── Recurring jobs ────────────────────────────────────────────────────────
    if (!app.Environment.IsEnvironment("Testing"))
        app.ScheduleRecurringJobs();

    // ── Middleware pipeline ───────────────────────────────────────────────────

    app.UseExceptionHandler();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSerilogRequestLogging(opts =>
    {
        opts.GetLevel = (httpContext, _, _) =>
            httpContext.Request.Path.StartsWithSegments("/health")
                ? LogEventLevel.Verbose
                : LogEventLevel.Information;
    });

    app.UseAuthentication();
    app.UseAuthorization();

    // ── Endpoints ─────────────────────────────────────────────────────────────
    app.MapRazorPages();

    app.MapControllers();
    app.MapHub<AuthorizationGraphHub>(HubRoutes.AuthorizationGraph);
    app.MapModules();

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

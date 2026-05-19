using AegisIdentity.Api.Endpoints.Dev;
using AegisIdentity.Api.Middleware;
using AegisIdentity.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

// ─── Two-stage Serilog initialization ────────────────────────────────────────
// Bootstrap logger captures startup errors before the DI container is ready.
// The real logger (from appsettings) is configured after WebApplication.CreateBuilder.
//
// SENSITIVE DATA POLICY — fields that MUST NEVER appear as structured log arguments:
//   Password, PasswordHash, Token, AccessToken, RefreshToken, ResetCode, Secret.
// These are not scrubbed automatically; enforcement is by convention and code review.
// A destructuring policy or log sink filter can be added in a future security hardening card.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting AegisIdentity API");

    var builder = WebApplication.CreateBuilder(args);

    // Replace bootstrap logger with the full logger defined in appsettings.
    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext());

    // Infrastructure configuration — bound and validated at startup.
    // Any missing required value throws OptionsValidationException before the app accepts requests.
    builder.Services.AddInfrastructureOptions(builder.Configuration);

    builder.Services.AddRazorPages();

    var app = builder.Build();

    // ─── Production hardening: reject localhost SMTP ──────────────────────────
    // Prevents accidental use of the dev Mailpit relay (localhost:1025) in production,
    // which would silently drop all outbound emails. Cost: ~5 lines. Risk avoided: high.
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

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    // CorrelationId must run before request logging so the log entry includes the field.
    app.UseMiddleware<CorrelationIdMiddleware>();

    // Log every HTTP request. Health check paths are downgraded to Verbose
    // so they do not pollute dashboards. When /health endpoints are implemented
    // (a future card), this filter is already in place.
    app.UseSerilogRequestLogging(opts =>
    {
        opts.GetLevel = (httpContext, _, _) =>
            httpContext.Request.Path.StartsWithSegments("/health")
                ? LogEventLevel.Verbose
                : LogEventLevel.Information;
    });

    app.MapRazorPages();

    // ─── Development-only endpoints ───────────────────────────────────────────
    // These routes are never registered in Staging or Production.
    if (app.Environment.IsDevelopment())
    {
        EmailTestEndpoint.Map(app);
    }

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

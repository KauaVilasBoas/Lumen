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
    builder.Services.AddSecurity();       // BCryptPasswordHasher, JwtService, PasswordValidator
    builder.Services.AddHibpClient();     // IPwnedPasswordsClient
    builder.Services.AddNotifications(); // IEmailService, IEmailTemplateRenderer

    // ── Application (MediatR + FluentValidation) ──────────────────────────────
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<RegisterUserCommandHandler>();
        // ValidationBehavior runs all IValidator<TRequest> before each handler.
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    });

    // Register all AbstractValidator<T> nested in the CommandHandlers assembly.
    builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserCommandHandler>();

    // ── Presentation ──────────────────────────────────────────────────────────
    builder.Services.AddControllers();

    // ValidationExceptionHandler maps FluentValidation.ValidationException → 400 ProblemDetails.
    builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
    builder.Services.AddProblemDetails();

    // ── Health checks ─────────────────────────────────────────────────────────
    builder.Services
        .AddHealthChecks()
        .AddCheck<MongoDbHealthCheck>("mongodb");

    builder.Services.AddRazorPages();

    var app = builder.Build();

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

    if (app.Environment.IsDevelopment())
    {
        // Dev-only endpoints (e.g. DevController) are part of MapControllers above.
        // The guard is enforced by DevController's own ApiExplorerSettings and the
        // conditional Swagger registration below (when added in a future iteration).
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

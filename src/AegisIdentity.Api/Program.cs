using AegisIdentity.Api.Endpoints.Auth;
using AegisIdentity.Api.Endpoints.Dev;
using AegisIdentity.Api.Middleware;
using AegisIdentity.Application.Auth;
using AegisIdentity.Application.Security;
using AegisIdentity.CommandHandlers.Auth.Register;
using AegisIdentity.Infrastructure.Configuration;
using AegisIdentity.DataAccess.HealthChecks;
using AegisIdentity.DataAccess.Persistence;
using AegisIdentity.Integration.Notifications;
using AegisIdentity.Integration.Security;
using AegisIdentity.Infrastructure.Security;
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

    builder.Services.AddInfrastructureOptions(builder.Configuration);
    builder.Services.AddMongoDb(builder.Configuration);
    builder.Services.AddSecurity();
    builder.Services.AddHibpClient();
    builder.Services.AddNotifications();
    builder.Services.AddApplicationSecurity();
    builder.Services.AddAuthValidators();

    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssemblyContaining<RegisterUserCommandHandler>());

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

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
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

    app.MapRazorPages();

    app.MapHealthChecks("/health/db", new HealthCheckOptions
    {
        Predicate = registration => registration.Name == "mongodb",
    });

    RegisterEndpoint.Map(app);
    LoginEndpoint.Map(app);

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

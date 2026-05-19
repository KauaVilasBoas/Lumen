# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added (DATA-01)
- **`MongoDbContext`** (`src/AegisIdentity.Infrastructure/Persistence/MongoDbContext.cs`):
  - Singleton wrapper over `IMongoDatabase` exposing `GetCollection<T>(name)` and a
    `Database` property for raw-command access.
  - Owns one-time `ConventionPack` registration via a double-checked lock:
    `CamelCaseElementNameConvention`, `IgnoreExtraElementsConvention(true)`,
    `EnumRepresentationConvention(BsonType.String)`.
- **`MongoDbServiceExtensions`** (`src/AegisIdentity.Infrastructure/Persistence/MongoDbServiceExtensions.cs`):
  - `AddMongoDb(IServiceCollection, IConfiguration)` extension method.
  - Registers `IMongoClient` as **singleton** (driver manages its own connection pool),
    `IMongoDatabase` as **scoped** (cheap factory from the singleton client, aligned with
    unit-of-work boundaries), and `MongoDbContext` as **singleton**.
- **`MongoDbHealthCheck`** (`src/AegisIdentity.Infrastructure/HealthChecks/MongoDbHealthCheck.cs`):
  - Implements `IHealthCheck`; issues `{ ping: 1 }` against the configured database.
  - Returns `HealthCheckResult.Healthy` on success, `Unhealthy` with the caught exception
    on failure.
- **Health check endpoint** `GET /health/db` registered in `Program.cs`:
  - Returns `200 OK` with a JSON body when MongoDB is reachable, `503 Service Unavailable`
    when the ping fails.
  - Requests to `/health` are already downgraded to `Verbose` in `UseSerilogRequestLogging`,
    so health probes do not pollute dashboards.
- **Integration tests** (`tests/AegisIdentity.IntegrationTests/Persistence/MongoDbContextIntegrationTests.cs`):
  - Four scenarios using `Testcontainers.MongoDb` (ephemeral `mongo:7` container):
    `GetCollection` handle validation, insert-and-read roundtrip, health check Healthy
    path, health check Unhealthy path.
  - Requires Docker Desktop with the daemon accessible to the test process
    (TCP on `localhost:2375` or membership in the `docker-users` group).

### Added (SETUP-05)
- **`docker-compose.yml`** at project root defining two services for the local development stack:
  - `mailpit` (`axllent/mailpit:latest`): catches all outbound emails from the app and exposes
    them in a web UI. SMTP on `localhost:1025`; UI on http://localhost:8025.
    Messages are **not persisted** — intentional design decision to keep dev inboxes clean
    between restarts. Persistence can be re-enabled by uncommenting the volume in the compose file.
  - `mongo` (`mongo:7`): local MongoDB instance on `localhost:27017` with a named volume
    `mongo-data` for data persistence across restarts. Includes a `healthcheck` using
    `mongosh --eval 'db.runCommand({ ping: 1 })'` so dependent services know when Mongo is ready.
- **`.mailpit-data/` added to `.gitignore`** to prevent accidental commit of Mailpit data
  if the optional persistence volume is ever re-enabled.
- **Production hardening** in `Program.cs`: the app throws `InvalidOperationException` at startup
  if `Smtp:Host` resolves to a loopback address (`localhost`, `127.0.0.1`, `::1`) when
  `ASPNETCORE_ENVIRONMENT=Production`. Prevents silent email loss from a misconfigured deploy.
- **Dev-only email smoke test endpoint** (`GET /dev/email-test?to=<address>`):
  - Registered at `src/AegisIdentity.Api/Endpoints/Dev/EmailTestEndpoint.cs`.
  - Available **only** when `ASPNETCORE_ENVIRONMENT=Development` — never in Staging or Production.
  - Sends a plain-text message through the configured SMTP relay (Mailpit in dev) and returns
    `200 { "ok": true, "to": "...", "viewer": "http://localhost:8025" }` on success,
    `500` with error detail on failure.
  - Uses `SmtpOptions` injected from DI — no hard-coded SMTP settings.
  - Inline Minimal API style; no domain coupling.

### Added (SETUP-04)
- **Serilog two-stage initialization** in `Program.cs`: bootstrap logger captures startup
  errors before DI is ready; full logger (from `appsettings`) takes over after
  `WebApplication.CreateBuilder`. Fatal exceptions are caught and flushed before exit.
- **Structured logging configuration** in `appsettings.json`:
  - Minimum levels: `Information` default, `Warning` for `Microsoft.AspNetCore` and `System`,
    `Information` for `Microsoft.AspNetCore.Hosting.Diagnostics`.
  - Console sink with `CompactJsonFormatter` (production-ready JSON).
  - File sink: `logs/aegis-.log`, daily rolling, 7-day retention, `CompactJsonFormatter`.
  - Enrichers: `FromLogContext`, `WithMachineName`, `WithThreadId`.
- **Development log override** in `appsettings.Development.json`:
  - Console only (no file sink), `Debug` minimum level, human-readable `outputTemplate`.
- **`CorrelationIdMiddleware`** (`src/AegisIdentity.Api/Middleware/`):
  - Reads `X-Correlation-Id` request header; generates a 32-char hex Guid when absent.
  - Writes value to response header and pushes it to Serilog `LogContext` so every log
    entry in the request scope carries `CorrelationId`.
  - Registered before `UseSerilogRequestLogging` so the request-completion log includes the field.
- **Health check log filter** in `UseSerilogRequestLogging`: requests to `/health` are
  logged at `Verbose` level (will not appear under default `Information` minimum) to
  avoid dashboard pollution when health-check endpoints are implemented.
- **`SensitiveDataConvention`** (`src/AegisIdentity.Api/Logging/`):
  - Static class documenting which fields (`Password`, `PasswordHash`, `Token`,
    `AccessToken`, `RefreshToken`, `ResetCode`, `Secret`) must never be passed as
    structured log arguments.
  - Enforcement is by convention and code review at this stage. A destructuring policy
    or log-sink filter will be added in the security hardening card when the corresponding
    use cases exist. Premature defensive code was intentionally omitted (YAGNI).
- **New NuGet packages** pinned in `Directory.Packages.props`:
  - `Serilog.Formatting.Compact` 3.0.0
  - `Serilog.Enrichers.Environment` 3.0.1
  - `Serilog.Enrichers.Thread` 4.0.0
- **Unit tests** for `CorrelationIdMiddleware` (4 scenarios):
  - Generates new ID when header is absent.
  - Preserves incoming ID when header is present.
  - Always sets response header.
  - Generated ID matches 32-char hex format (Guid "N").


### Added
- `JwtOptions`, `MongoOptions`, `SmtpOptions`, `HibpOptions` in `Infrastructure/Configuration/`
  with `[Required]`, `[MinLength]`, `[Range]` and `[Url]` data-annotation constraints.
- Startup validation via `AddOptions<T>().Bind(...).ValidateDataAnnotations().ValidateOnStart()`
  registered in `InfrastructureOptionsExtensions`. Any missing required value causes an
  `OptionsValidationException` before the app begins accepting requests.
- Full `appsettings.json` schema with safe `REPLACE_ME` placeholders covering:
  `Mongo`, `Jwt`, `Smtp`, `Hibp`, `Cors` and `Serilog` sections.
- `appsettings.Development.json` with local dev defaults: Mongo on `mongodb://localhost:27017`
  and SMTP on Mailpit (`localhost:1025`).
- `appsettings.example.json` versionable template documenting all keys and their expected
  format with `<set via env or user-secrets>` placeholders.
- `UserSecretsId` already present in `AegisIdentity.Api.csproj` from SETUP-01 bootstrap;
  confirmed operational via `dotnet user-secrets list`.
- README: table of required environment variables, local setup via `dotnet user-secrets`,
  and production configuration via env vars (Fly.io / Docker formats).

### Added
- Central Package Management: all MVP NuGet dependencies pinned in `Directory.Packages.props`.
  - **Persistence:** `MongoDB.Driver` 2.30.0
  - **Auth / Security:** `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.15,
    `System.IdentityModel.Tokens.Jwt` 8.9.0, `BCrypt.Net-Next` 4.0.3
  - **Validation:** `FluentValidation` 11.11.0, `FluentValidation.AspNetCore` 11.3.0
  - **Email:** `MailKit` 4.16.0 (MimeKit is a transitive dependency)
  - **API / Docs:** `Swashbuckle.AspNetCore` 8.1.1
  - **Observability:** `Serilog.AspNetCore` 9.0.0, `Serilog.Sinks.Console` 6.0.0,
    `Serilog.Sinks.File` 6.0.0
  - **HTTP / Integrations:** `Microsoft.Extensions.Http` 8.0.1
  - **Testing:** `xunit` 2.9.3, `xunit.runner.visualstudio` 2.8.2,
    `coverlet.collector` 6.0.4, `Microsoft.NET.Test.Sdk` 17.13.0,
    `FluentAssertions` 7.2.0, `NSubstitute` 5.3.0,
    `Microsoft.AspNetCore.Mvc.Testing` 8.0.15, `Testcontainers.MongoDb` 4.4.0
- `PackageReference` entries wired per project following Clean Architecture boundaries:
  - `Domain`: zero external packages (pure domain model)
  - `Application`: `FluentValidation` only (no ASP.NET Core dependency)
  - `Infrastructure`: MongoDB, BCrypt, MailKit, Serilog, HttpClient, JWT token library
  - `Api`: JwtBearer, FluentValidation.AspNetCore, Swashbuckle, Serilog.AspNetCore
  - `UnitTests`: xunit, FluentAssertions, NSubstitute
  - `IntegrationTests`: everything in UnitTests + Mvc.Testing + Testcontainers.MongoDb

### Decisions
- **Rate Limiting:** Native `Microsoft.AspNetCore.RateLimiting` middleware (ASP.NET Core 7+)
  is included in the `Microsoft.AspNetCore.App` shared framework — no additional NuGet
  package needed for `Sdk="Microsoft.NET.Sdk.Web"` projects. The third-party
  `AspNetCoreRateLimit` package was intentionally omitted. Revisit in SEC-01 if
  native middleware proves insufficient.

### Changed
- Renamed `AegisIdentity.Backoffice` to `AegisIdentity.Api` to align project name
  with Clean Architecture entry-point convention (hosts both Minimal API endpoints
  and Razor Pages backoffice UI).
- Centralized build settings (`Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`,
  `LangVersion`) in `Directory.Build.props` — individual csproj files are now minimal.
- Activated Central Package Management via `Directory.Packages.props` to enforce
  a single version source of truth for all NuGet dependencies.

### Added
- `Directory.Build.props` — solution-wide MSBuild properties.
- `Directory.Packages.props` — Central Package Management.
- `LICENSE` — MIT license.

### Fixed
- `.gitignore` extended with `appsettings.*.local.json` and `secrets.json` patterns.

## [0.1.0] - 2026-05-18

### Added
- Initial Clean Architecture skeleton: `Api`, `Application`, `Domain`, `Infrastructure`,
  `UnitTests`, `IntegrationTests`.
- Razor Pages backoffice entrypoint with Serilog structured logging.
- `.gitignore`, `.editorconfig` and base solution file.

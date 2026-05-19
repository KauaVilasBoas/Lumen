# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

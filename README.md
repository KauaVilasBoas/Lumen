<h1 align="center">AegisIdentity</h1>

<p align="center">
  <i>Identity & Authentication service in .NET 8 — multi-solution Clean Architecture with CQRS, ports & adapters, a Razor MVC backoffice consuming its own JWT, and recurring jobs on Hangfire.</i>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 8"/>
  &nbsp;
  <img src="https://img.shields.io/badge/Architecture-Clean%20%2B%20CQRS-A78BFA?style=flat-square" alt="Clean Architecture + CQRS"/>
  &nbsp;
  <img src="https://img.shields.io/badge/Mediator-MediatR-blue?style=flat-square" alt="MediatR"/>
  &nbsp;
  <img src="https://img.shields.io/badge/Database-SQL%20Server-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white" alt="SQL Server"/>
  &nbsp;
  <img src="https://img.shields.io/badge/Cache-Redis-DC382D?style=flat-square&logo=redis&logoColor=white" alt="Redis"/>
  &nbsp;
  <img src="https://img.shields.io/badge/Auth-JWT%20%2B%20Permission--based-000000?style=flat-square&logo=jsonwebtokens&logoColor=white" alt="JWT + Permission-based Auth"/>
  &nbsp;
  <img src="https://img.shields.io/badge/Jobs-Hangfire-EC4899?style=flat-square" alt="Hangfire"/>
  &nbsp;
  <img src="https://img.shields.io/badge/Tested-xUnit-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt="xUnit"/>
  &nbsp;
  <img src="https://img.shields.io/badge/Status-Active%20Development-43B581?style=flat-square" alt="Active Development"/>
  &nbsp;
  <img src="https://img.shields.io/badge/License-MIT-blue?style=flat-square" alt="MIT"/>
</p>

---

## Why this exists

A portfolio piece built to demonstrate **end-to-end architectural decisions on a non-trivial domain** — not just "another auth API". Every choice is deliberate: layer boundaries, dependency direction, validation strategy, error contracts, and operational concerns. The goal is to make the reasoning visible, not to ship the smallest possible MVP.

---

## Architecture at a glance

```mermaid
flowchart TB
    CLIENT(["🌐 HTTP Client"])
    BROWSER(["👤 Admin Browser"])

    subgraph PRES["🎯 Presentation"]
        API["<b>AegisIdentity.Api</b><br/><sub>Controllers · IMediator</sub>"]
        BO["<b>AegisIdentity.Backoffice</b><br/><sub>ASP.NET MVC · cookie auth + JWT</sub>"]
    end

    subgraph APP["⚡ Application · CQRS via MediatR"]
        PIPE["<b>ValidationBehavior</b><br/><sub>FluentValidation pipeline · fail-fast</sub>"]
        CMD["<b>CommandHandlers</b><br/><sub>RegisterUser · LoginUser</sub>"]
        QRY["<b>ReadModels</b><br/><sub>Query handlers (scaffold)</sub>"]
        EVT["<b>EventHandlers</b><br/><sub>Notification handlers (scaffold)</sub>"]
    end

    subgraph DOM["💎 Domain · zero external dependencies"]
        ENT["<b>Entities & Value Objects</b><br/>User · RefreshToken<br/>EmailConfirmationToken"]
        PORT["<b>Ports (interfaces)</b><br/>IUserRepository · IJwtService<br/>IPasswordHasher · IEmailService"]
    end

    subgraph INFRA["🔧 Infrastructure · adapters"]
        SEC["<b>Infrastructure</b><br/><sub>JWT · BCrypt · PasswordValidator · Options</sub>"]
        DAL["<b>DataAccess</b><br/><sub>EF Core DbContext · Repositories<br/>Migrations · Redis permission cache</sub>"]
        INT["<b>Integration</b><br/><sub>MailKit · PwnedPasswordsClient (HIBP)</sub>"]
    end

    subgraph JOB["⏰ Jobs · Hangfire + Hangfire.SqlServer"]
        HF["<b>AegisIdentity.Jobs</b><br/><sub>Recurring scheduler</sub>"]
        CLEAN["<b>CleanupExpiredRefreshTokensJob</b><br/><sub>cron · 03:00 UTC daily</sub>"]
    end

    DB[("🗄️ <b>SQL Server</b><br/>AegisIdentity (schema dbo)<br/>HangFire (schema HangFire)")]
    CACHE[("⚡ <b>Redis</b><br/>user:permissions:{userId}")]
    SMTP[/"📧 SMTP server"/]
    HIBP[/"🛡️ HaveIBeenPwned API<br/><sub>k-anonymity range query</sub>"/]

    CLIENT -- "POST /api/auth/register" --> API
    BROWSER -- "session cookie" --> BO
    BO -- "AuthApiClient · Bearer JWT" --> API
    BO -- "/hangfire dashboard" --> HF

    API -- "Command" --> PIPE
    PIPE -- "validated" --> CMD
    CMD --> ENT
    CMD -. "depends on" .-> PORT

    SEC -. "implements" .-> PORT
    DAL -. "implements" .-> PORT
    INT -. "implements" .-> PORT

    DAL ==> |"EF Core · async I/O"| DB
    DAL ==> |"permissions cache"| CACHE
    INT -. "SendAsync" .-> SMTP
    INT -. "range API" .-> HIBP

    HF --> CLEAN
    CLEAN -. "DeleteExpiredAsync" .-> DAL
    HF ==> |"job storage"| DB

    classDef domain     fill:#512BD4,stroke:#A78BFA,color:#fff,stroke-width:2px
    classDef pres       fill:#0b1220,stroke:#3b82f6,color:#dbeafe
    classDef app        fill:#0b1220,stroke:#10b981,color:#d1fae5
    classDef infra      fill:#0b1220,stroke:#f59e0b,color:#fde68a
    classDef jobs       fill:#0b1220,stroke:#ec4899,color:#fce7f3
    classDef external   fill:#020617,stroke:#64748b,color:#cbd5e1
    classDef db         fill:#CC2927,stroke:#ff6b6b,color:#ffffff,stroke-width:3px
    classDef cache      fill:#DC382D,stroke:#ff8c8c,color:#ffffff,stroke-width:2px

    class ENT,PORT domain
    class API,BO pres
    class PIPE,CMD,QRY,EVT app
    class SEC,DAL,INT infra
    class HF,CLEAN jobs
    class CLIENT,BROWSER,SMTP,HIBP external
    class DB db
    class CACHE cache

    linkStyle 0  stroke:#3b82f6,stroke-width:2px
    linkStyle 1  stroke:#3b82f6,stroke-width:2px
    linkStyle 2  stroke:#3b82f6,stroke-width:2px
    linkStyle 3  stroke:#ec4899,stroke-width:2px
    linkStyle 4  stroke:#10b981,stroke-width:2px
    linkStyle 5  stroke:#10b981,stroke-width:2px
    linkStyle 6  stroke:#a78bfa,stroke-width:2px
    linkStyle 7  stroke:#a78bfa,stroke-width:2px,stroke-dasharray:4 4
    linkStyle 8  stroke:#a78bfa,stroke-width:2px,stroke-dasharray:4 4
    linkStyle 9  stroke:#a78bfa,stroke-width:2px,stroke-dasharray:4 4
    linkStyle 10 stroke:#a78bfa,stroke-width:2px,stroke-dasharray:4 4
    linkStyle 11 stroke:#00ed64,stroke-width:3.5px
    linkStyle 12 stroke:#f59e0b,stroke-width:2px,stroke-dasharray:4 4
    linkStyle 13 stroke:#f59e0b,stroke-width:2px,stroke-dasharray:4 4
    linkStyle 14 stroke:#ec4899,stroke-width:2px
    linkStyle 15 stroke:#ec4899,stroke-width:2px,stroke-dasharray:4 4
    linkStyle 16 stroke:#00ed64,stroke-width:3.5px
```

<sub><b>Reading the diagram</b> — solid arrows are in-process synchronous flow · dashed arrows are port/adapter wiring and external I/O · thick green arrows are MongoDB read/write paths · Domain (purple) has zero outgoing dependencies; every other layer depends inward on it.</sub>

---

## Engineering decisions

| Decision | Rationale |
|---|---|
| **Multi-solution Clean Architecture** | Six layer solutions (`Domain` · `Application` · `Infrastructure` · `Presentation` · `Jobs` · `SharedKernel`) aggregated by a root `.sln`. Each layer can be opened, built and reasoned about in isolation. |
| **CQRS via MediatR with nested types** | `Command`, `Result`, `Validator` are `sealed record`s nested inside the handler. One file = one use case, fully self-contained. No anemic DTO layer between Controller and Handler. |
| **`ValidationBehavior<TRequest,TResponse>` pipeline** | FluentValidation runs *before* the handler. Cheap input checks fail fast; I/O-bound rules (uniqueness, HIBP) stay inside `Handle()`. |
| **Ports & Adapters (Dependency Inversion)** | All infrastructure contracts (`IUserRepository`, `IJwtService`, `IEmailService`, `IPasswordHasher`…) live in **Domain**. Adapters in Infrastructure are wired by DI — Domain has zero external references, verified by the compiler. |
| **SQL Server + EF Core (relational authz)** | Migrated from MongoDB to SQL Server with EF Core 8. Enables foreign-key integrity between User, Profile, Permission and token entities. Migrations are version-controlled — no runtime seed, no ad-hoc schema changes. See [ADR-0001](docs/adr/0001-mongodb-to-relational-efcore.md). |
| **Permission-based authorization** | `[RequirePermission]` attribute on API actions triggers discovery at startup, Redis-cached resolution at request time, and a dynamic `IAuthorizationPolicyProvider` for enforcement. See [Authorization model](docs/authz.md). |
| **Redis distributed permission cache** | Permission sets are cached per user in Redis with event-driven invalidation (`UserPermissionsChanged`). When Redis is unavailable, the enforcement layer falls back to the database — authorization never fails open. |
| **Soft-delete everywhere** | No entity is ever physically deleted. EF Core global query filters hide deleted records; a filtered unique index on `Email`/`Username` (`WHERE IsDeleted = 0`) allows re-registration after soft-delete. |
| **Razor Backoffice consumes its own JWT** | The MVC backoffice authenticates against the public API via a typed `AuthApiClient`, stores the JWT inside an HttpOnly cookie session, and exposes the Hangfire dashboard guarded by cookie auth. |
| **Hangfire + Hangfire.SqlServer for recurring work** | The API hosts the Hangfire server; jobs are scheduled on startup. `CleanupExpiredRefreshTokensJob` removes expired refresh tokens daily at 03:00 UTC via the same `IRefreshTokenRepository` port. |
| **Central Package Management** | `Directory.Packages.props` is the single source of truth for NuGet versions across all 13 projects. Zero orphans, zero drift. |
| **`TreatWarningsAsErrors` + nullable enabled globally** | Quality bar enforced by the compiler from `Directory.Build.props`. No "we'll clean it up later". |
| **Startup-time options validation** | `ValidateDataAnnotations().ValidateOnStart()` — misconfiguration crashes on boot, never silently in production. |
| **Global `IExceptionHandler` → RFC 7807** | `ValidationException` → `ValidationProblemDetails 400`; `ConflictException` → `ProblemDetails 409`. Consistent, machine-readable errors. |
| **xUnit + Testcontainers integration tests** | 360+ unit tests; integration suite spins up real SQL Server and Redis containers via Testcontainers. Tests are a first-class deliverable, not an afterthought. |

---

## Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 / ASP.NET Core 8 |
| API | Controllers + MediatR (CQRS) |
| Backoffice | ASP.NET Core MVC (Razor) |
| Auth | JWT Bearer + Cookie auth + Permission-based authorization |
| Validation | FluentValidation 11 + MediatR pipeline behavior |
| Crypto | BCrypt.Net-Next |
| Background jobs | Hangfire + Hangfire.SqlServer |
| Email | MailKit |
| Database | SQL Server + EF Core 8 (migrations, soft-delete, FK integrity) |
| Cache | Redis (distributed permission cache, event-driven invalidation) |
| Logging | Serilog (structured, JSON in prod) |
| Testing | xUnit + Testcontainers (SQL Server + Redis) |
| Local dev | Docker Compose (Mailpit + SQL Server + Redis) |
| Deploy | Railway (API + Backoffice as long-running .NET containers) |

---

## Solution layout

```
AegisIdentity/
├── src/
│   ├── AegisIdentity.Api/                          Presentation — Controllers, Hangfire server host
│   ├── AegisIdentity.Domain/                       Entities, value objects, ports (interfaces)
│   │                                               ↳ Authorization: Permission, Profile, UserProfile, PermissionProfile, GroupPermission
│   ├── AegisIdentity.Infrastructure/               Cross-cutting: JWT, BCrypt, PasswordValidator, Options, HealthChecks
│   ├── Application/
│   │   ├── AegisIdentity.CommandHandlers/          Register, Login, Profile/UserProfile CRUD + Validators + ValidationBehavior
│   │   ├── AegisIdentity.EventHandlers/            UserPermissionsChanged → cache invalidation
│   │   └── AegisIdentity.ReadModels/               GetCurrentUserQuery (/me), ListProfiles, ListPermissions, ListUserProfiles
│   ├── Infrastructure/
│   │   ├── AegisIdentity.DataAccess/               EF Core DbContext, SQL Server repositories, Redis permission cache
│   │   └── AegisIdentity.Integration/              MailKitEmailService, PwnedPasswordsClient, templates
│   ├── Migrations/
│   │   ├── AegisIdentity.Migrations/               EF Core migrations (schema + data migrations for admin/profiles)
│   │   └── AegisIdentity.Migrations.Cli/           dotnet ef wrapper CLI
│   ├── Presentation/
│   │   └── AegisIdentity.Backoffice/               MVC backoffice (cookie auth → API JWT, RequirePermissionTagHelper, HasPermissionAsync)
│   ├── Jobs/
│   │   └── AegisIdentity.Jobs/                     Hangfire + Hangfire.SqlServer, recurring jobs
│   └── SharedKernel/
│       └── AegisIdentity.SharedKernel/             RequirePermissionAttribute, ControllerNameNormalizer, Base64UrlEncoder, Sha256Hasher
├── tests/
│   ├── AegisIdentity.UnitTests/                    Domain + Application (360+ passing)
│   └── AegisIdentity.IntegrationTests/             Api + Infrastructure (Testcontainers: SQL Server + Redis)
├── docs/
│   ├── authz.md                                    Authorization model, soft-delete, /me contract, Backoffice helpers
│   └── adr/
│       ├── 0001-mongodb-to-relational-efcore.md    ADR: SQL Server migration, Railway deploy, Vercel exclusion
│       └── 0002-admin-bootstrap-credential.md      ADR: admin bootstrap credential via data migration
├── docker-compose.yml                              Local dev stack (Mailpit + SQL Server + Redis)
├── Directory.Build.props                           Global MSBuild settings
├── Directory.Packages.props                        Central Package Management
└── AegisIdentity.sln                               Root aggregator (organizes projects into solution folders)
```

Each layer also ships its own `.sln` (`Domain.sln`, `Application.sln`, `Infrastructure.sln`, `Presentation.sln`, `Jobs.sln`, `SharedKernel.sln`) — they are views over the same physical `.csproj` set, allowing layer-scoped builds and reviews.

---

## Getting started

### Prerequisites

- .NET 8 SDK
- Docker Desktop (for `docker compose up`)

### Local development with Docker

The `docker-compose.yml` at the repository root brings up three services with a single command:

| Service | Purpose | Endpoint |
|---|---|---|
| Mailpit | Local SMTP + Web UI to inspect outbound emails | SMTP `localhost:1025` / UI `http://localhost:8025` |
| SQL Server 2022 | Local database | `localhost:1433` (SA password in `docker-compose.yml`) |
| Redis 7 | Distributed permission cache | `localhost:6379` |

```powershell
# Start all services in the background
docker compose up -d

# Stop without removing SQL Server data
docker compose down

# Stop AND wipe the SQL Server volume (full reset)
docker compose down -v
```

> **Mailpit does not persist messages.** Each `docker compose down` clears the inbox — a deliberate decision to avoid confusion between development sessions. Edit `docker-compose.yml` and uncomment the `.mailpit-data` volume to enable persistence.

### Apply database migrations

EF Core migrations must be applied before the first run. Migrations are applied automatically
at startup via `Database.Migrate()`, but you can also apply them manually:

```powershell
dotnet run --project src/Migrations/AegisIdentity.Migrations.Cli
```

The initial data migrations insert:
- The bootstrap admin user (`admin@aegisidentity.local`, id `10000000-0000-0000-0000-000000000001`).
- The system profiles `Administrator` and `User`.
- The `UserProfile` binding the admin user to the `Administrator` profile.

See [ADR-0002](docs/adr/0002-admin-bootstrap-credential.md) for the bootstrap credential policy.

> **There is no runtime seed command.** All initial business data arrives via EF Core data migrations.
> The only runtime initialization is the additive permission reconciliation for the Administrator
> profile, which runs automatically on startup after discovery.

### Run the application

```powershell
dotnet restore
dotnet run --project src/AegisIdentity.Api
```

The API starts on `http://localhost:5237` (HTTP) or `https://localhost:7068` (HTTPS).

### Email smoke test

With the containers running, exercise the dev-only email endpoint:

```powershell
curl "http://localhost:5237/dev/email-test?to=you@test.com"

# Expected response
# { "ok": true, "to": "you@test.com", "viewer": "http://localhost:8025" }
```

Then open `http://localhost:8025` to confirm the message arrived.

> The `/dev/email-test` endpoint is registered **only** when `ASPNETCORE_ENVIRONMENT=Development`. It never appears in Staging or Production.

---

## Configuration

### Required environment variables

| Variable (env var format) | Section : Key | Description | Example |
|---|---|---|---|
| `SqlServer__ConnectionString` | `SqlServer:ConnectionString` | SQL Server connection string | `Server=localhost,1433;Database=AegisIdentity;User Id=sa;Password=...;TrustServerCertificate=True` |
| `Redis__ConnectionString` | `Redis:ConnectionString` | Redis connection string | `localhost:6379` |
| `Jwt__Issuer` | `Jwt:Issuer` | JWT issuer | `AegisIdentity` |
| `Jwt__Audience` | `Jwt:Audience` | JWT audience | `AegisIdentity.Clients` |
| `Jwt__Secret` | `Jwt:Secret` | HMAC-SHA256 signing key (min 32 chars) | `<strong-random-key>` |
| `Jwt__ExpirationMinutes` | `Jwt:ExpirationMinutes` | Access token lifetime, in minutes | `15` |
| `Jwt__RefreshExpirationDays` | `Jwt:RefreshExpirationDays` | Refresh token lifetime, in days | `7` |
| `Smtp__Host` | `Smtp:Host` | SMTP server | `smtp.sendgrid.net` |
| `Smtp__Port` | `Smtp:Port` | SMTP port | `587` |
| `Smtp__User` | `Smtp:User` | SMTP user | `apikey` |
| `Smtp__Pass` | `Smtp:Pass` | SMTP password / API key | `<secret>` |
| `Smtp__From` | `Smtp:From` | Sender address | `no-reply@yourdomain.com` |
| `Smtp__UseStartTls` | `Smtp:UseStartTls` | Enable STARTTLS | `true` |
| `Hibp__UserAgent` | `Hibp:UserAgent` | User-Agent for the HIBP API | `YourApp/1.0 (contact@yourdomain.com)` |
| `Hibp__ApiBaseUrl` | `Hibp:ApiBaseUrl` | HIBP API base URL | `https://api.pwnedpasswords.com` |
| `Cors__AllowedOrigins__0` | `Cors:AllowedOrigins[0]` | Allowed CORS origin | `https://yourdomain.com` |
| `App__BaseUrl` | `App:BaseUrl` | Public API base URL (no trailing slash) — used in outbound email links | `https://api.yourdomain.com` |

> Every `[Required]` option fails the startup if missing (`ValidateOnStart`). Misconfiguration crashes the app on boot, never silently in production.

### Backoffice required configuration

The Backoffice (`src/Presentation/AegisIdentity.Backoffice`) depends on three infrastructure
services. All are validated on startup with `ValidateOnStart` — a missing or empty value fails
fast on boot.

| Variable (env var format) | Section : Key | Description | Example |
|---|---|---|---|
| `Api__BaseUrl` | `Api:BaseUrl` | Base URL of the AegisIdentity API (no trailing slash) | `https://api.aegisidentity.io` |
| `SqlServer__ConnectionString` | `SqlServer:ConnectionString` | SQL Server connection string — same database as the API | `Server=localhost,1433;...` |
| `Redis__ConnectionString` | `Redis:ConnectionString` | Redis connection string — required for the permission cache (`IUserPermissionCache`) | `localhost:6379` |

> **Redis is a required dependency of the Backoffice** (introduced in FIX-04 / INFRA-07).
> The permission cache used by `RequirePermissionTagHelper` and `HasPermissionAsync` reads
> from Redis on every request. Start Redis with `docker compose up -d redis` before running
> the Backoffice locally.

### Local development via User Secrets

Use `dotnet user-secrets` to store local secrets without committing them:

```powershell
# API
cd src/AegisIdentity.Api

dotnet user-secrets set "SqlServer:ConnectionString" "Server=localhost,1433;Database=AegisIdentity;User Id=sa;Password=Dev@AegisIdentity2024!;TrustServerCertificate=True"
dotnet user-secrets set "Redis:ConnectionString" "localhost:6379"
dotnet user-secrets set "Jwt:Secret" "<your-random-key-at-least-32-chars>"
dotnet user-secrets set "Smtp:Host" "localhost"
dotnet user-secrets set "Smtp:Port" "1025"
dotnet user-secrets set "Smtp:From" "no-reply@aegisidentity.local"

# Backoffice
cd src/Presentation/AegisIdentity.Backoffice

dotnet user-secrets set "SqlServer:ConnectionString" "Server=localhost,1433;Database=AegisIdentity;User Id=sa;Password=Dev@AegisIdentity2024!;TrustServerCertificate=True"
dotnet user-secrets set "Redis:ConnectionString" "localhost:6379"
dotnet user-secrets set "Api:BaseUrl" "https://localhost:7068"
```

Secrets live in `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` and never enter the repository.

### Production configuration (env vars)

In production, inject secrets via the Railway dashboard (or equivalent hosting provider).
ASP.NET Core maps `Section__Key` to `Section:Key` automatically:

```bash
# Railway — set via the service variables panel or CLI
SqlServer__ConnectionString=Server=<railway-sqlserver-host>;Database=AegisIdentity;User Id=sa;Password=<secret>;TrustServerCertificate=True
Redis__ConnectionString=<railway-redis-host>:6379,password=<secret>
Jwt__Secret=your-strong-production-key-min-32-chars
Smtp__Host=smtp.sendgrid.net
Smtp__Pass=SG.xxxxxxxxxxxxxxxxxxxxx

# Alternative: Azure SQL Database (serverless/free tier)
# The EF Core provider is the same — only the connection string changes.
SqlServer__ConnectionString=Server=<azure-sql>.database.windows.net;Database=AegisIdentity;...
```

> The API and Backoffice are deployed as long-running .NET container services on **Railway**.
> Vercel is not used — it does not support long-running .NET runtimes, SQL Server, or Redis.
> See [ADR-0001](docs/adr/0001-mongodb-to-relational-efcore.md) for the full rationale.

> **Never** put real secrets in `appsettings.json` or `appsettings.Development.json`. See `src/AegisIdentity.Api/appsettings.example.json` for the full configuration shape.

---

## Logging

### Format per environment

| Environment | Sink | Format |
|---|---|---|
| Production | Console + rolling file (daily) | `CompactJsonFormatter` (structured JSON) |
| Development | Console only | Human-readable: `[HH:mm:ss LVL] Message {Properties}` |

Log files live in `logs/aegis-YYYYMMDD.log`, retained for 7 days.

### Correlation ID

Every request gets an `X-Correlation-Id`. If the header arrives in the request, the value is preserved. If absent, the middleware generates a Guid in the `N` format (32 hex chars). The ID is attached to every log line for the request and to the response header `X-Correlation-Id`.

To trace a specific request:

```powershell
curl -H "X-Correlation-Id: my-trace-id" https://localhost:7068/
```

### Sensitive-data policy

The following fields must **never** appear as structured log arguments:

- `Password`, `PasswordHash`
- `Token`, `AccessToken`, `RefreshToken`
- `ResetCode`, `Secret`

Only safe fields (`Email`, `UserId`, etc.) should be logged. See `src/AegisIdentity.Api/Logging/SensitiveDataConvention.cs` for the full list and examples. Enforcement is currently by convention and code review; an automated filter is planned alongside the security hardening card once the relevant use cases land.

---

## Password policy

Every password accepted by the system (registration, change, reset) is validated by `IPasswordValidator` (implementation in `src/AegisIdentity.Infrastructure/Security/`). The rules:

- Minimum **12 characters**.
- At least **one uppercase letter**, **one lowercase letter**, **one digit** and **one special character** from ``!@#$%^&*()-_=+[]{};:'",.<>/?\|`~``.
- Must not equal the user's email or username (case-insensitive).
- Must not appear in the **HaveIBeenPwned Pwned Passwords** dataset.

The HIBP check uses the **k-anonymity** model: `PwnedPasswordsClient` sends only the first 5 hex characters of `SHA1(password)` to `https://api.pwnedpasswords.com/range/{prefix}` (with `Add-Padding: true`). Results are cached in memory for **1 hour** per prefix.

The HIBP client is **fail-open**: timeouts or upstream errors do not block registration — they emit a structured `Warning` and the password is accepted. This is a deliberate trade-off: an external dependency outage must never deny access to our own system. The residual risk is tracked in `SEC-05`.

Error messages are returned one rule per line — the user sees everything they need to fix in a single response.

---

## Running tests

```powershell
# Everything, except tests that hit external services
dotnet test

# Includes the integration test that calls the public HaveIBeenPwned API
dotnet test --filter "Category=ExternalApi"
```

---

## Authorization

AegisIdentity uses a **permission-based authorization model** — permissions are derived from
API actions, grouped into profiles, and assigned to users at runtime.

### Permission flow

```
Permission (Controller.Action)
    └── PermissionProfile (join)
            └── Profile ("Administrator", "User", custom…)
                    └── UserProfile (join)
                            └── User
```

### Protecting an endpoint

Decorate any API action with `[RequirePermission]`:

```csharp
[HttpDelete("{id}")]
[RequirePermission]
public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { ... }
```

The permission code `Controller.Action` is derived automatically. On the next startup,
discovery registers the permission and the startup reconciler grants it to the Administrator
profile. Assign it to other profiles via the Backoffice UI.

- **401** — request is not authenticated (fallback policy requires authentication by default).
- **403** — request is authenticated but the user lacks the permission.
- Use `[AllowAnonymous]` to opt out of the fallback authentication requirement.

### Backoffice Razor helpers

```cshtml
@* Conditional block *@
@if (await Html.HasPermissionAsync("Profiles", "Delete"))
{
    <a asp-action="Delete" asp-route-id="@item.Id">Delete</a>
}

@* TagHelper — suppresses the element entirely when permission is absent *@
<div asp-require-permission-controller="Profiles"
     asp-require-permission-action="Create">
    <a asp-action="Create">New Profile</a>
</div>
```

### Permission cache

User permissions are cached in Redis under `user:permissions:{userId}`. Invalidation is
event-driven (`UserPermissionsChanged`). When Redis is unavailable the enforcement layer
falls back to the database — **authorization never fails open**.

For the complete model reference (entities, soft-delete, `/me` contract, data model diagram,
ADRs), see [docs/authz.md](docs/authz.md).

---

## API surface

| Method | Route | Description | Status |
|---|---|---|---|
| `POST` | `/api/auth/register` | Register a new user and send a confirmation email | Available |
| `GET`  | `/api/auth/confirm-email` | Confirm the email via token | Planned (AUTH-10) |
| `POST` | `/api/auth/login` | Authenticate and return JWT + refresh token | Available |
| `GET`  | `/api/me` | Return the authenticated user's profile and profile memberships | Available |
| `GET`  | `/api/profiles` | List all profiles | Available |
| `POST` | `/api/profiles` | Create a profile | Available |
| `PUT`  | `/api/profiles/{id}` | Update a profile | Available |
| `DELETE` | `/api/profiles/{id}` | Soft-delete a profile | Available |
| `GET`  | `/api/permissions` | List discovered permissions (grouped) | Available |
| `GET`  | `/api/user-profiles` | List user-profile assignments | Available |
| `POST` | `/api/user-profiles` | Assign a profile to a user | Available |
| `DELETE` | `/api/user-profiles/{id}` | Soft-delete a user-profile assignment | Available |
| `GET`  | `/health/db` | SQL Server + Redis health check | Available |
| `GET`  | `/dev/email-test` | Email smoke test (Development only) | Available |

### Register a user

```powershell
curl -X POST http://localhost:5237/api/auth/register `
  -H "Content-Type: application/json" `
  -d '{"email":"you@example.com","username":"you","password":"Str0ng!Passw0rd-2026"}'

# 201 Created
# { "id": "...", "email": "you@example.com", "username": "you" }
```

The user is created with `isActive = false`. A confirmation email is sent automatically.

### Authenticate a user

```powershell
# The "identifier" field accepts either email or username
curl -X POST http://localhost:5237/api/auth/login `
  -H "Content-Type: application/json" `
  -d '{"identifier":"you@example.com","password":"Str0ng!Passw0rd-2026"}'

# 200 OK
# { "accessToken": "<jwt>", "refreshToken": "<opaque>", "expiresIn": 900 }
```

Possible response codes:

| Code | Meaning |
|---|---|
| `200` | Login succeeded — returns `accessToken`, `refreshToken` and `expiresIn` |
| `400` | Validation failed — `identifier` or `password` is blank |
| `401` | Invalid credentials — user does not exist or password is wrong (deliberately opaque to prevent enumeration) |
| `403` | Email is not confirmed |
| `423` | Account locked after repeated failures — wait and retry |

---

## Roadmap

| Card | Description | Status |
|---|---|---|
| SETUP-01 | Layered solution structure | Done |
| SETUP-02 | Central NuGet package management | Done |
| SETUP-03 | Environment variables and `appsettings` | Done |
| SETUP-04 | Serilog structured logging | Done |
| SETUP-05 | Local Mailpit via Docker Compose | Done |
| DATA-01  | MongoDB context and health check | Done |
| DATA-02  | `User` aggregate modelling and persistence | Done |
| DATA-03  | Token aggregates (refresh, reset, confirmation) | Done |
| SEC-04   | Strong password policy | Done |
| SEC-05   | HaveIBeenPwned Pwned Passwords integration | Done |
| EMAIL-01 | Email delivery via MailKit | Done |
| AUTH-01  | `POST /api/auth/register` | Done |
| AUTH-02  | `POST /api/auth/login` | Done |
| ARCH-01  | Multi-solution Clean Architecture + CQRS refactor | Done |
| JOBS-01  | Hangfire recurring jobs (token cleanup) | Done |
| INFRA-01 | ADR: SQL Server + EF Core migration | Done |
| INFRA-02..06 | MongoDB → SQL Server + EF Core + Redis migration | Done |
| AUTH-08  | Domain model: Permission, Profile, UserProfile, PermissionProfile | Done |
| AUTH-09  | Permission discovery at startup (`[RequirePermission]` scanning) | Done |
| AUTH-10  | `GET /api/auth/confirm-email` | Planned |
| AUTH-11  | Redis permission cache + event-driven invalidation | Done |
| AUTH-12  | Default profiles and admin binding via data migration + startup reconciliation | Done |
| AUTH-13  | Drop RBAC roles column; fallback authorization policy | Done |
| AUTH-14  | Backoffice `HasPermissionAsync` helper + `RequirePermissionTagHelper` | Done |
| AUTH-15  | Profile/permission/user-profile CRUD (API + Backoffice) | Done |
| AUTH-16  | `/me` exposes profiles `{ id, name }` instead of roles | Done |
| DOC-01   | Authz model documentation (`docs/authz.md` + README update + CHANGELOG) | Done |
| OPS-01   | GitHub Actions CI (`dotnet build` + `dotnet test`) | Planned |

See [`TASKS_TRELLO.md`](./TASKS_TRELLO.md) for the full backlog.

---

## Known limitations

- Email confirmation (`AUTH-10`) is not implemented yet — the link is generated at registration, but the confirmation endpoint does not exist.
- Refresh-token rotation is not yet implemented — refresh use case is on the roadmap.
- No per-IP rate limiting on the registration endpoint (dedicated card pending).
- No CI/CD pipeline configured (tracked as `OPS-01`).
- No public deployment yet.
- HTTPS is not configured in dev — defaults to local HTTP via `launchSettings`.
- `/dev/email-test` depends on the Mailpit container being up (`docker compose up -d`). `IEmailService` is fail-open: the endpoint returns `200` even if SMTP is unreachable — open `http://localhost:8025` to confirm delivery. SMTP failures are logged as `Warning`.
- The bootstrap admin password (`ADR-0002`) must be rotated before any non-development deployment — there is no forced password-change flow on first login yet.
- Permission codes are derived from controller and action names at startup; renaming a controller without updating dependent code will mark the old permissions as orphans (visible via `GET /api/permissions`) until they are reassigned or the profile is updated.

---

## License

[MIT](LICENSE)

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
  <img src="https://img.shields.io/badge/Database-MongoDB-13AA52?style=flat-square&logo=mongodb&logoColor=white" alt="MongoDB"/>
  &nbsp;
  <img src="https://img.shields.io/badge/Auth-JWT-000000?style=flat-square&logo=jsonwebtokens&logoColor=white" alt="JWT"/>
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
        DAL["<b>DataAccess</b><br/><sub>MongoDbContext · Repositories<br/>ClassMaps · Index initializer</sub>"]
        INT["<b>Integration</b><br/><sub>MailKit · PwnedPasswordsClient (HIBP)</sub>"]
    end

    subgraph JOB["⏰ Jobs · Hangfire + Hangfire.Mongo"]
        HF["<b>AegisIdentity.Jobs</b><br/><sub>Recurring scheduler</sub>"]
        CLEAN["<b>CleanupExpiredRefreshTokensJob</b><br/><sub>cron · 03:00 UTC daily</sub>"]
    end

    DB[("🗄️ <b>MongoDB</b><br/>AegisIdentity_db<br/>AegisIdentity_hangfire")]
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

    DAL ==> |"BSON · async I/O"| DB
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
    classDef db         fill:#13aa52,stroke:#00ed64,color:#ffffff,stroke-width:3px

    class ENT,PORT domain
    class API,BO pres
    class PIPE,CMD,QRY,EVT app
    class SEC,DAL,INT infra
    class HF,CLEAN jobs
    class CLIENT,BROWSER,SMTP,HIBP external
    class DB db

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
| **Razor Backoffice consumes its own JWT** | The MVC backoffice authenticates against the public API via a typed `AuthApiClient`, stores the JWT inside an HttpOnly cookie session, and exposes the Hangfire dashboard guarded by cookie auth. |
| **Hangfire + Hangfire.Mongo for recurring work** | The API hosts the Hangfire server; jobs are scheduled on startup. `CleanupExpiredRefreshTokensJob` removes expired refresh tokens daily at 03:00 UTC via the same `IRefreshTokenRepository` port. |
| **Central Package Management** | `Directory.Packages.props` is the single source of truth for NuGet versions across all 13 projects. Zero orphans, zero drift. |
| **`TreatWarningsAsErrors` + nullable enabled globally** | Quality bar enforced by the compiler from `Directory.Build.props`. No "we'll clean it up later". |
| **Startup-time options validation** | `ValidateDataAnnotations().ValidateOnStart()` — misconfiguration crashes on boot, never silently in production. |
| **Global `IExceptionHandler` → RFC 7807** | `ValidationException` → `ValidationProblemDetails 400`; `ConflictException` → `ProblemDetails 409`. Consistent, machine-readable errors. |
| **xUnit + Testcontainers integration tests** | 222 unit tests; integration suite spins up a real MongoDB via Testcontainers. Tests are a first-class deliverable, not an afterthought. |

---

## Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 / ASP.NET Core 8 |
| API | Controllers + MediatR (CQRS) |
| Backoffice | ASP.NET Core MVC (Razor) |
| Auth | `Microsoft.AspNetCore.Authentication.JwtBearer` + Cookie auth |
| Validation | FluentValidation 11 + MediatR pipeline behavior |
| Crypto | BCrypt.Net-Next |
| Background jobs | Hangfire + Hangfire.Mongo |
| Email | MailKit |
| Database | MongoDB |
| Logging | Serilog (structured, JSON in prod) |
| Testing | xUnit + Testcontainers |
| Local dev | Docker Compose (Mailpit + MongoDB) |

---

## Solution layout

```
AegisIdentity/
├── src/
│   ├── AegisIdentity.Api/                          Presentation — Controllers, Hangfire server host
│   ├── AegisIdentity.Domain/                       Entities, value objects, ports (interfaces)
│   ├── AegisIdentity.Infrastructure/               Cross-cutting: JWT, BCrypt, PasswordValidator, Options, HealthChecks
│   ├── Application/
│   │   ├── AegisIdentity.CommandHandlers/          Register, Login + Validators + ValidationBehavior
│   │   ├── AegisIdentity.EventHandlers/            Notification handlers (scaffold)
│   │   └── AegisIdentity.ReadModels/               Query handlers (scaffold)
│   ├── Infrastructure/
│   │   ├── AegisIdentity.DataAccess/               MongoDbContext, repositories, ClassMaps, MongoDbHealthCheck
│   │   └── AegisIdentity.Integration/              MailKitEmailService, PwnedPasswordsClient, templates
│   ├── Presentation/
│   │   └── AegisIdentity.Backoffice/               MVC backoffice (cookie auth → API JWT)
│   ├── Jobs/
│   │   └── AegisIdentity.Jobs/                     Hangfire configuration + recurring jobs
│   └── SharedKernel/
│       └── AegisIdentity.SharedKernel/             Constants, util helpers (Base64UrlEncoder, Sha256Hasher)
├── tests/
│   ├── AegisIdentity.UnitTests/                    Domain + Application (222 passing)
│   └── AegisIdentity.IntegrationTests/             Api + Infrastructure (Testcontainers)
├── docker-compose.yml                              Local dev stack (Mailpit + MongoDB)
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

The `docker-compose.yml` at the repository root brings up two services with a single command:

| Service | Purpose | Endpoint |
|---|---|---|
| Mailpit | Local SMTP + Web UI to inspect outbound emails | SMTP `localhost:1025` / UI `http://localhost:8025` |
| MongoDB | Local database | `mongodb://localhost:27017` |

```powershell
# Start both services in the background
docker compose up -d

# Stop without removing Mongo data
docker compose down

# Stop AND wipe the Mongo volume (full reset)
docker compose down -v
```

> **Mailpit does not persist messages.** Each `docker compose down` clears the inbox — a deliberate decision to avoid confusion between development sessions. Edit `docker-compose.yml` and uncomment the `.mailpit-data` volume to enable persistence.

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
| `Mongo__ConnectionString` | `Mongo:ConnectionString` | MongoDB connection URI | `mongodb://localhost:27017` |
| `Mongo__Database` | `Mongo:Database` | Database name | `aegisidentity` |
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

### Local development via User Secrets

Use `dotnet user-secrets` to store local secrets without committing them:

```powershell
cd src/AegisIdentity.Api

dotnet user-secrets set "Mongo:ConnectionString" "mongodb://localhost:27017"
dotnet user-secrets set "Mongo:Database" "aegisidentity_dev"
dotnet user-secrets set "Jwt:Secret" "<your-random-key-at-least-32-chars>"
dotnet user-secrets set "Smtp:Host" "localhost"
dotnet user-secrets set "Smtp:Port" "1025"
dotnet user-secrets set "Smtp:From" "no-reply@aegisidentity.local"
```

Secrets live in `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` and never enter the repository.

### Production configuration (env vars)

In production, inject secrets via the hosting provider's environment variables (Fly.io, Railway, etc.). ASP.NET Core maps `Section__Key` to `Section:Key` automatically:

```bash
# Fly.io
fly secrets set Mongo__ConnectionString="mongodb+srv://user:pass@cluster/dbname"
fly secrets set Jwt__Secret="your-strong-production-key-min-32-chars"
fly secrets set Smtp__Host="smtp.sendgrid.net"
fly secrets set Smtp__Pass="SG.xxxxxxxxxxxxxxxxxxxxx"

# Docker / docker-compose
environment:
  - Mongo__ConnectionString=mongodb://mongo:27017
  - Jwt__Secret=your-strong-key
  - Smtp__Host=mailserver
```

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

## API surface

| Method | Route | Description | Status |
|---|---|---|---|
| `POST` | `/api/auth/register` | Register a new user and send a confirmation email | Available |
| `GET`  | `/api/auth/confirm-email` | Confirm the email via token | Planned (AUTH-10) |
| `POST` | `/api/auth/login` | Authenticate and return JWT + refresh token | Available |
| `GET`  | `/health/db` | MongoDB health check | Available |
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
| AUTH-10  | `GET /api/auth/confirm-email` | Planned |
| AUTH-11  | Refresh-token rotation | Planned |
| OPS-01   | GitHub Actions CI (`dotnet build` + `dotnet test`) | Planned |

See [`TASKS_TRELLO.md`](./TASKS_TRELLO.md) for the full backlog.

---

## Known limitations

- Email confirmation (`AUTH-10`) is not implemented yet — the link is generated at registration, but the endpoint does not exist.
- Refresh-token rotation is not yet implemented — refresh use case is on the roadmap.
- No per-IP rate limiting on the registration endpoint (dedicated card pending).
- No CI/CD pipeline configured (tracked as `OPS-01`).
- No public deployment yet.
- HTTPS is not configured in dev — defaults to local HTTP via `launchSettings`.
- `/dev/email-test` depends on the Mailpit container being up (`docker compose up -d`). `IEmailService` is fail-open: the endpoint returns `200` even if SMTP is unreachable — open `http://localhost:8025` to confirm delivery. SMTP failures are logged as `Warning`.

---

## License

[MIT](LICENSE)

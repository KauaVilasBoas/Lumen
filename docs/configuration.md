# Configuration reference

Every `[Required]` option is validated with `ValidateDataAnnotations().ValidateOnStart()` —
misconfiguration crashes the app on boot, never silently in production.

## API — required environment variables

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

## Backoffice — required environment variables

The Backoffice (`src/Presentation/AegisIdentity.Backoffice`) depends on three infrastructure
services, all validated on startup.

| Variable (env var format) | Section : Key | Description | Example |
|---|---|---|---|
| `Api__BaseUrl` | `Api:BaseUrl` | Base URL of the AegisIdentity API (no trailing slash) | `https://api.aegisidentity.io` |
| `SqlServer__ConnectionString` | `SqlServer:ConnectionString` | SQL Server connection string — same database as the API | `Server=localhost,1433;...` |
| `Redis__ConnectionString` | `Redis:ConnectionString` | Redis connection string — required for the permission cache (`IUserPermissionCache`) | `localhost:6379` |

> **Redis is a required dependency of the Backoffice** (introduced in FIX-04 / INFRA-07).
> The permission cache used by `RequirePermissionTagHelper` and `HasPermissionAsync` reads
> from Redis on every request. Start Redis with `docker compose up -d redis` before running
> the Backoffice locally.

## Local development via User Secrets

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

## Production configuration (env vars)

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
> See [ADR-0001](adr/0001-mongodb-to-relational-efcore.md) for the full rationale.

> **Never** put real secrets in `appsettings.json` or `appsettings.Development.json`.
> See `src/AegisIdentity.Api/appsettings.example.json` for the full configuration shape.

## Logging

### Format per environment

| Environment | Sink | Format |
|---|---|---|
| Production | Console + rolling file (daily) | `CompactJsonFormatter` (structured JSON) |
| Development | Console only | Human-readable: `[HH:mm:ss LVL] Message {Properties}` |

Log files live in `logs/aegis-YYYYMMDD.log`, retained for 7 days.

### Correlation ID

Every request gets an `X-Correlation-Id`. If the header arrives in the request, the value is
preserved. If absent, the middleware generates a Guid in the `N` format (32 hex chars). The ID
is attached to every log line for the request and to the response header `X-Correlation-Id`.

```powershell
curl -H "X-Correlation-Id: my-trace-id" https://localhost:7068/
```

### Sensitive-data policy

The following fields must **never** appear as structured log arguments:

- `Password`, `PasswordHash`
- `Token`, `AccessToken`, `RefreshToken`
- `ResetCode`, `Secret`

Only safe fields (`Email`, `UserId`, etc.) should be logged. See
`src/AegisIdentity.Api/Logging/SensitiveDataConvention.cs` for the full list and examples.
Enforcement is currently by convention and code review; an automated filter is planned
alongside the security hardening card once the relevant use cases land.

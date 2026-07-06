# Lumen.Identity

Pluggable authentication library for ASP.NET Core (.NET 8).
Provides user registration, login, JWT/refresh-token rotation, email confirmation,
password reset/change, lockout, and integration bridges for `Lumen.Authorization`.

## Packages

| Package | Purpose |
|---|---|
| `Lumen.Identity` | Core: user domain, CQRS handlers, JWT/BCrypt/MailKit adapters, authorization bridges |
| `Lumen.Identity.AspNetCore` | ASP.NET Core integration: `AddLumenIdentity()` umbrella and `MapLumenIdentityEndpoints()` minimal-API endpoints |
| `Lumen.Identity.Migrations` | EF Core migrations for the `identity` schema (SQL Server); hosted service applies them on startup |
| `Lumen.Identity.Migrations.PostgreSQL` | EF Core migrations for the `identity` schema (PostgreSQL, snake_case); hosted service applies them on startup |

> **Requires `Lumen.Authorization >= 1.1.0`.**  
> `Lumen.Identity` depends on the Authorization library for the `IAuthorizationUserSource`
> and `IUserDirectory` integration contracts.

## Minimum wiring in `Program.cs`

```csharp
// AddLumenIdentity registers: Identity core, JWT Bearer, Authorization core, and migrations
builder.Services.AddLumenIdentity(builder.Configuration);

// ...

app.UseAuthentication();
app.UseAuthorization();

// Minimal-API endpoints: /identity/login, /identity/register, /identity/refresh, etc.
app.MapLumenIdentityEndpoints();
```

> SQL Server or PostgreSQL is required. Provide the connection string via
> `IConfiguration["ConnectionStrings:DefaultConnection"]`.

## Available endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/identity/login` | Authenticate user, returns JWT + refresh token |
| `POST` | `/identity/refresh` | Rotate refresh token, returns new JWT |
| `POST` | `/identity/logout` | Revoke refresh token |
| `POST` | `/identity/register` | Create user account |
| `GET` | `/identity/me` | Return current user info (requires auth) |
| `POST` | `/identity/confirm-email` | Confirm email address |
| `POST` | `/identity/resend-confirmation` | Resend confirmation email |
| `POST` | `/identity/forgot-password` | Request password reset email |
| `POST` | `/identity/reset-password` | Apply password reset token |
| `POST` | `/identity/change-password` | Change password (requires auth) |

## Multi-provider database support

```csharp
// SQL Server (default)
builder.Services.AddLumenIdentity(builder.Configuration);

// PostgreSQL — use the PostgreSQL migrations package instead
builder.Services.AddLumenIdentity(builder.Configuration, options =>
    options.Provider = DatabaseProvider.PostgreSQL);
```

## Repository & documentation

- Source: <https://github.com/KauaVilasBoas/Lumen>
- Architecture decision (Authorization as library): [ADR-0004](https://github.com/KauaVilasBoas/Lumen/blob/main/docs/adr/0004-authorization-as-library.md)
- Architecture decision (tenant-scoped authorization): [ADR-0006](https://github.com/KauaVilasBoas/Lumen/blob/main/docs/adr/0006-authz-tenant-scoped-permissions.md)

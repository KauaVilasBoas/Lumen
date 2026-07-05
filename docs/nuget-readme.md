# Lumen.Authorization

Pluggable role-based authorization library for ASP.NET Core (.NET 8).
Manages Permissions, Profiles (roles), and User↔Profile assignments,
backed by SQL Server and exposed via a mountable Backoffice UI.

## Packages

| Package | Purpose |
|---|---|
| `Lumen.Authorization` | Core: domain, CQRS handlers, EF Core persistence, DI root |
| `Lumen.Authorization.Contracts` | Public interfaces (`IUserPermissionService`, `IUserIdAccessor`) and exported events |
| `Lumen.Authorization.Migrations` | EF Core migrations for the `Lumen` schema; hosted service applies them on startup |
| `Lumen.Authorization.AspNetCore` | ASP.NET Core glue: `[RequirePermission]`, policy provider, discovery, enforcement |
| `Lumen.Authorization.Backoffice` | Razor Class Library with Profiles & Permissions management UI |

## Minimum wiring in `Program.cs`

```csharp
// Umbrella: registers core, migrations, enforcement, and discovery in one call
builder.Services.AddLumenAuthorization(builder.Configuration);

// Optional: mountable Backoffice UI
builder.Services.AddLumenBackoffice();

// ...

app.UseStaticFiles();      // serves _content/Lumen.Authorization.Backoffice/
app.UseAuthentication();
app.UseAuthorization();
app.MapLumenBackoffice("/lumen");
```

> SQL Server is required. Provide the connection string via `IConfiguration["ConnectionStrings:DefaultConnection"]`
> or use the string overload: `AddLumenAuthorization("Server=...;Database=...;")`.

## Protecting an endpoint

```csharp
// Convention: permission code = "Controller.Action"
[RequirePermission]
public IActionResult Index() { ... }

// Explicit code
[RequirePermission("Profiles.Delete")]
public IActionResult Delete(Guid id) { ... }
```

## Repository & documentation

- Source: <https://github.com/KauaVilasBoas/Lumen>
- Consumer guide: `docs/authz-library.md` in the repository
- Architecture decision: [ADR-0004](https://github.com/KauaVilasBoas/Lumen/blob/main/docs/adr/0004-authorization-as-library.md)

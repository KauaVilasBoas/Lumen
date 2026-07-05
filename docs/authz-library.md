# Lumen.Authorization — guia do consumidor

Visão geral dos pacotes e instruções para integrar a biblioteca de autorização Lumen
em uma aplicação ASP.NET Core.

---

## Requisito de banco de dados: SQL Server obrigatório

`Lumen.Authorization` suporta **exclusivamente SQL Server**. As migrations em
`Lumen.Authorization.Migrations` são SQL-Server-específicas e o registro usa `UseSqlServer`
internamente.

Fornecer uma connection string nula, vazia ou que não seja parseável como SQL Server causa
uma `ArgumentException` imediata no startup — intencionalmente, para falhar cedo e com mensagem
legível, em vez de propagar um erro obscuro do EF Core.

Suporte a outros providers de banco de dados (ex.: PostgreSQL) é uma evolução futura planejada
e não está no escopo dos cards LIB-xx atuais.

---

## Pacotes e seus papéis

| Pacote | Papel |
|---|---|
| `Lumen.Authorization` | Núcleo: domínio, aplicação (Commands/Queries), persistência (EF Core + SQL Server), DI root |
| `Lumen.Authorization.Contracts` | Interfaces públicas (`IUserPermissionService`, `IUserIdAccessor`) e eventos exportados |
| `Lumen.Authorization.Migrations` | Migrations EF Core do schema `Lumen`; hosted service que as aplica no startup |
| `Lumen.Authorization.AspNetCore` | Cola com ASP.NET Core: `[RequirePermission]`, policy provider, discovery, `AddLumenAuthorizationEnforcement()`, `AddLumenAuthorizationDiscovery()` |
| `Lumen.Authorization.Backoffice` | Razor Class Library com UI de gestão de Perfis e Permissões; `AddLumenBackoffice()` + `MapLumenBackoffice(prefix)` |

---

## Wiring mínimo em `Program.cs`

```csharp
// 1. Núcleo + persistência (SQL Server obrigatório)
builder.Services.AddLumenAuthorization(builder.Configuration, o =>
{
    o.ApplyMigrationsOnStartup = true;   // padrão — cria/atualiza schema Lumen
    o.RedisConnectionString = "...";     // opcional — fallback para MemoryCache
});

// 2. Migrations (hosted service que aplica as migrations no startup)
builder.Services.AddLumenAuthorizationMigrations();

// 3. Enforcement ASP.NET Core ([RequirePermission], policy provider)
builder.Services.AddLumenAuthorizationEnforcement();

// 4. Discovery e reconciliação (descobre [RequirePermission] e sincroniza no banco)
builder.Services.AddLumenAuthorizationDiscovery();

// 5. Backoffice montável (opcional)
builder.Services.AddLumenBackoffice();

// ...

app.UseStaticFiles();           // serve _content/Lumen.Authorization.Backoffice/
app.UseAuthentication();
app.UseAuthorization();
app.MapLumenBackoffice("/lumen");
```

---

## `LumenAuthorizationOptions`

| Propriedade | Padrão | Descrição |
|---|---|---|
| `ApplyMigrationsOnStartup` | `true` | Aplica migrations do schema `Lumen` ao iniciar |
| `RedisConnectionString` | `null` | Se ausente, usa `IDistributedMemoryCache` |
| `UserIdClaimType` | `ClaimTypes.NameIdentifier` | Claim de onde o userId (`Guid`) é lido |

---

## Proteger um endpoint

```csharp
// Convenção automática: código = "Controller.Action"
[RequirePermission]
public IActionResult Index() { ... }

// Código explícito
[RequirePermission("Profiles.Delete")]
public IActionResult Delete(Guid id) { ... }

// Policy nomeada (ex.: middleware customizado)
[Authorize(Policy = "Lumen:Profiles.Delete")]
public IActionResult Delete(Guid id) { ... }
```

---

## Relação Usuário ↔ Perfil

`UserProfile.UserId` é um `Guid` opaco — a lib não impõe modelo de usuário.

- Implemente `IAuthorizationUserSource` para listar usuários no backoffice.
- Implemente `IUserDirectory` para resolver nomes de usuário para exibição.
- Bootstrap do primeiro administrador: use `AssignUserProfileCommand` com
  `SystemProfiles.AdministratorId`.

---

## ADR de referência

- [ADR-0004](adr/0004-authorization-as-library.md) — decisão de extrair autorização como biblioteca plugável.

# ADR-0005: Suporte multi-provider de banco de dados (SQL Server e PostgreSQL)

**Status:** Accepted — 2026-07-05
**Related:** ADR-0004 (autorização como biblioteca), card #146 "[Infra] Suporte a PostgreSQL (persistência multi-provider)"

---

## Contexto

A ADR-0004 entregou a família `Lumen.Authorization*` com `LumenAuthorizationDbContext`
vinculado exclusivamente ao SQL Server via `UseSqlServer(...)`. A lib exigia que o consumidor
fornecesse uma connection string SQL Server, e um guard (`SqlConnectionStringBuilder`) rejeitava
qualquer string não parseável por esse dialeto.

Isso criava dois bloqueios concretos:

1. **Adoção limitada:** consumidores em cloud com PostgreSQL gerenciado (Supabase, Neon,
   Railway, etc.) não conseguiam usar a lib sem substituir o provider.
2. **Portabilidade zero:** as migrations (`Lumen.Authorization.Migrations`) usavam tipos
   SQL-Server-específicos (`uniqueidentifier`, `nvarchar`, `datetime2`, `bit`) e filtros de
   índice com sintaxe `[Column] = 0` inválida no PostgreSQL.

O objetivo deste ADR é documentar as decisões que desbloqueiam o suporte a PostgreSQL
**sem quebrar o caminho SQL Server existente**.

---

## Decisão

### 1. Enum `DatabaseProvider` e opção no `LumenAuthorizationOptions`

Introduzir o enum `DatabaseProvider { SqlServer, PostgreSQL }` (namespace `Lumen.Authorization`)
e a propriedade `Provider` em `LumenAuthorizationOptions` (padrão: `DatabaseProvider.SqlServer`).
Retrocompatibilidade total: consumidores existentes que não configuram `Provider` continuam no
SQL Server sem nenhuma mudança.

### 2. Guard condicional por provider

O método `ValidateConnectionString` substitui o antigo `ValidateSqlServerConnectionString`:

- `DatabaseProvider.SqlServer` → valida com `SqlConnectionStringBuilder` (comportamento anterior).
- `DatabaseProvider.PostgreSQL` → valida com `NpgsqlConnectionStringBuilder`.
- Ambos os caminhos rejeitam strings nulas/vazias na mesma mensagem de erro.

O guard é **best-effort** (parse estático): protege contra erros de configuração comuns;
a validação definitiva ocorre na primeira conexão ao banco.

### 3. Seleção de provider via `IConfiguration`

O overload `AddLumenAuthorization(IConfiguration, ...)` lê `Database:Provider` (case-insensitive)
de qualquer fonte de configuração (appsettings, env vars, secrets). Ausência da chave mantém
o padrão SQL Server.

```json
// appsettings.json — caminho PostgreSQL
{
  "Database": { "Provider": "PostgreSQL" },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=lumen;Username=postgres;Password=secret"
  }
}
```

### 4. Filtros de índice provider-aware no DbContext

Os `HasFilter(...)` SQL-Server-específicos (`[IsDeleted] = 0`) foram removidos das classes
`IEntityTypeConfiguration<T>` e centralizados em
`LumenAuthorizationDbContext.ApplyProviderAwareConfigurations(modelBuilder, isPostgres)`, que
seleciona a sintaxe correta no momento da configuração do modelo:

| Dialeto      | Filtro aplicado         |
|--------------|-------------------------|
| SQL Server   | `[IsDeleted] = 0`       |
| PostgreSQL   | `is_deleted = false`    |

A detecção de provider é feita via `Database.ProviderName` (contém `"Npgsql"` → PostgreSQL).
Isso funciona tanto em runtime quanto em design-time (EF migrations).

### 5. Assembly separado para migrations PostgreSQL

Criado `Lumen.Authorization.Migrations.PostgreSQL` com migrations idiomáticas:

| Aspecto          | SQL Server                     | PostgreSQL                        |
|------------------|-------------------------------|-----------------------------------|
| UUID             | `uniqueidentifier`            | `uuid`                            |
| Texto            | `nvarchar(N)`                 | `character varying(N)`            |
| Data/hora        | `datetime2`                   | `timestamp with time zone`        |
| Booleano         | `bit`                         | `boolean`                         |
| Filtro de índice | `[IsDeleted] = 0`             | `is_deleted = false`              |
| Down seed        | `DELETE FROM [Lumen].[...]`   | `DELETE FROM "Lumen"."..."`       |

O assembly SQL Server (`Lumen.Authorization.Migrations`) permanece **intacto e inalterado**.
O consumidor referencia apenas o assembly compatível com seu provider:

```
SqlServer  → Lumen.Authorization + Lumen.Authorization.Migrations
PostgreSQL → Lumen.Authorization + Lumen.Authorization.Migrations.PostgreSQL
```

O `RegisterDbContext` escolhe o assembly correto via `LumenAuthorizationMigrationsAssembly`:

```csharp
case DatabaseProvider.PostgreSQL:
    dbOptions.UseNpgsql(cs, n => n.MigrationsAssembly(LumenAuthorizationMigrationsAssembly.PostgreSQL));
```

### 6. Hosted service de migrations por assembly

`LumenAuthorizationPostgresMigrationsHostedService` (interno ao pacote PostgreSQL) é o
equivalente do `LumenAuthorizationMigrationsHostedService` do SQL Server. O consumidor
registra o hosted service correto via:

- SQL Server: `AddLumenAuthorizationMigrations()` (pacote `Lumen.Authorization.Migrations`)
- PostgreSQL: `AddLumenAuthorizationPostgresMigrations()` (pacote `Lumen.Authorization.Migrations.PostgreSQL`)

---

## Como escolher o provider

### Código (`string` overload)

```csharp
// SQL Server (padrão — sem mudança para consumidores existentes)
services.AddLumenAuthorization("Server=...;Database=...;Trusted_Connection=True;");

// PostgreSQL
services.AddLumenAuthorization(
    "Host=localhost;Database=lumen;Username=postgres;Password=secret",
    o => o.Provider = DatabaseProvider.PostgreSQL);
```

### Configuração (`IConfiguration` overload)

```csharp
// Program.cs — automático via appsettings.json / env vars
services.AddLumenAuthorization(builder.Configuration);
```

```json
// appsettings.json
{
  "Database": { "Provider": "PostgreSQL" },
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Database=lumen;Username=app;Password=..."
  }
}
```

### Migrations CLI (design-time)

```bash
# SQL Server (assembly existente)
dotnet ef migrations add <Nome> \
  --project src/Lumen.Authorization.Migrations \
  --startup-project src/Lumen.Api

# PostgreSQL (novo assembly)
dotnet ef migrations add <Nome> \
  --project src/Lumen.Authorization.Migrations.PostgreSQL \
  --startup-project src/Lumen.Api
```

---

## Alternativas consideradas

### A. Provider único por pacote (forking total)

Criar pacotes separados `Lumen.Authorization.SqlServer` e `Lumen.Authorization.PostgreSQL`
com codebases distintos. Rejeitada: duplicação massiva de domínio, handlers e cache; maior
custo de manutenção; quebraria consumidores existentes do `Lumen.Authorization`.

### B. Runtime switching via `IOptions<>` sem enum

Ler o provider como `string` raw em runtime e usar `switch` sem tipagem. Rejeitada: menos
segura em tempo de compilação; não expressa o contrato no tipo.

### C. Migrations unificadas com SQL condicional

Uma única migration com `IF` e blocos condicionais por dialeto. Rejeitada: fragílidade,
legibilidade zero, e o EF Core não suporta migrations polimórficas nativamente.

---

## Consequências

### Positivas

- Consumidores PostgreSQL (Supabase, Neon, Railway, etc.) passam a ser suportados nativamente.
- Zero regressão para consumidores SQL Server: nenhuma mudança de interface pública.
- Migrations idiomáticas por dialeto: cada assembly tem o DDL correto para o banco-alvo.
- Testável sem banco vivo: os guards e o registro de DI são cobertos por unit tests.

### Negativas / trade-offs

- **Dois assemblies de migration** para manter em paralelo a cada nova migration de schema.
  Mitigação: a frequência de mudanças de schema de autorização é baixa após a estabilização.
- **Guard best-effort:** `NpgsqlConnectionStringBuilder` aceita algumas strings mal-formadas.
  A validação definitiva ocorre na primeira conexão.
- **Snapshot manual:** o `ModelSnapshot` PostgreSQL foi criado manualmente (sem `dotnet ef`
  contra banco vivo) — deve ser regenerado via `dotnet ef migrations add` após provisionar
  um banco Postgres de desenvolvimento.

### Projetos impactados

| Projeto | Impacto |
|---|---|
| `Lumen.Authorization` | Guard condicional; `UseNpgsql` no `RegisterDbContext`; filtros provider-aware no DbContext; Npgsql como dependência |
| `Lumen.Authorization.Migrations` | Sem mudanças — mantido intacto |
| `Lumen.Authorization.Migrations.PostgreSQL` | Novo assembly com migrations idiomáticas PostgreSQL |
| `Lumen.Authorization.Tests` | Testes atualizados + novos para provider PostgreSQL e IConfiguration |

# ADR-0001: Migração de MongoDB para banco relacional com EF Core

**Status:** Accepted — 2026-05-29

---

## Contexto

O AegisIdentity nasceu com MongoDB como único banco de dados. A escolha inicial priorizou
velocidade de bootstrap: schema flexível, sem migrations formais e driver simples. Com o
crescimento do domínio de identidade e a iminente introdução de autorização (RBAC/perfis),
esse modelo apresenta fricção crescente:

- **Integridade referencial ausente:** relações User ↔ RefreshToken ↔ PasswordResetToken
  ↔ EmailConfirmationToken não têm garantia de consistência no banco — apenas por convenção
  de código.
- **Joins inexistentes:** consultas cruzadas (ex.: User ↔ Profile ↔ Permission) exigem
  múltiplos round-trips ou desnormalização.
- **Migrations caseiras:** o `MongoMigrationsHostedService` é infraestrutura inventada
  localmente — sem suporte a rollback estruturado, sem tooling externo, sem histórico rastreável
  por convenção.
- **Hangfire.Mongo:** provider não-oficial com manutenção intermitente; o provider oficial
  `Hangfire.SqlServer` é mais estável e amplamente adotado.
- **Deploy**: Railway, plataforma-alvo, oferece SQL Server gerenciado como add-on de primeira
  classe. MongoDB em Railway exige add-on externo (Atlas) com tier free mais restrito e latência
  de rede adicional.

A janela de migração é agora — antes de o schema de autorização ser escrito —, pois reescrever
class maps Mongo e migrations existentes após AUTH-08..16 seria custo maior do que migrar
com o domínio ainda pequeno.

---

## Decisão

**Adotar SQL Server (EF Core 8) como único banco de dados da solução.**

Não haverá arquitetura poliglota (MongoDB para domínio + SQL para authz). Toda persistência
— usuários, tokens, jobs Hangfire, futuras entidades de autorização — reside em uma instância
SQL Server única. O provider EF Core utilizado é `Microsoft.EntityFrameworkCore.SqlServer`.

Esta decisão governa as tasks INFRA-02 a INFRA-06 e AUTH-08 a AUTH-16.

---

## Alternativas consideradas

### A1 — Manter MongoDB para domínio + SQL Server só para authz (poliglota)

Manteria a stack atual para o código já escrito e adicionaria SQL Server exclusivamente para
as entidades de autorização. Descartada porque:

- Duas stacks de migrations para um serviço só (Mongo runner caseiro + EF Core Migrations).
- Joins entre User e Profile/Permission impossíveis sem application-side join ou duplicação.
- Dois health checks, dois connection strings, dois contratos de deploy.
- O ganho (evitar reescrita dos repositórios Mongo) é menor do que o custo de manter dois
  paradigmas indefinidamente.

### A2 — PostgreSQL com EF Core (Npgsql)

PostgreSQL tem vantagens reais: tiers gerenciados free mais generosos (Supabase, Neon,
Railway Postgres), driver Npgsql maduro, e boa integração com EF Core. Foi seriamente
considerado. Descartado porque o usuário definiu SQL Server como requisito do projeto —
a familiaridade com T-SQL e o ecossistema Microsoft (.NET + SQL Server) é critério de
decisão explícito aqui.

### A3 — Vercel para hospedagem da API e/ou Backoffice

Descartada completamente. Vercel é plataforma serverless orientada a frontend (SPA,
Next.js, funções edge). Ela **não executa runtime ASP.NET de longa duração**, não hospeda
banco SQL Server e não serve containers .NET persistentes. O "frontend" atual do projeto é
o Backoffice Razor (`AegisIdentity.Backoffice`), que é uma aplicação ASP.NET MVC — exige
processo .NET em execução contínua, o que é incompatível com o modelo de execução da Vercel.
O que caberia na Vercel seria, no máximo, um SPA estático separado — que hoje não existe no
projeto. Portanto: API .NET, Backoffice Razor e banco SQL não têm espaço na Vercel.

### A4 — Azure SQL Database (serverless/free tier) como banco gerenciado de produção

Alternativa válida para o banco gerenciado de produção. O Azure SQL Database tem tier
serverless com pausa automática, compatível com SQL Server local de dev. Citado aqui como
opção real caso Railway SQL Server apresente limitações de tier. A decisão de plataforma
de cloud para o banco pode ser revisada em INFRA-02 sem impacto no código EF Core — o
provider é o mesmo (`Microsoft.EntityFrameworkCore.SqlServer`).

---

## Decisão detalhada

### 1. Engine e provider

| Aspecto | Decisão |
|---|---|
| Engine | SQL Server |
| Provider EF Core | `Microsoft.EntityFrameworkCore.SqlServer` |
| Versão EF Core | 8 (alinhado ao .NET 8 da solução) |
| Schema | `dbo` (padrão SQL Server, sem schema customizado) |
| Naming de tabelas | PascalCase singular (`User`, `RefreshToken`, `Permission`) |

### 2. Ambiente de desenvolvimento

Imagem Docker local:

```
mcr.microsoft.com/mssql/server:2022-latest
```

O ambiente de dev é Windows x64. O container SQL Server 2022 Linux x64 roda via Docker
Desktop sem necessidade de configuração adicional de arquitetura.

Redis local (cache distribuído, INFRA-06):

```
redis:7
```

Ambos são declarados em `docker-compose.dev.yml` (criado em INFRA-02).

### 3. Produção / deploy

- **Plataforma:** Railway.
- **API** (`AegisIdentity.Api`) e **Backoffice** (`AegisIdentity.Backoffice`): serviços
  Railway de longa duração (containers .NET).
- **Banco:** SQL Server gerenciado (Railway SQL Server add-on). Alternativa: Azure SQL
  Database serverless/free tier — mesmo provider EF Core, sem reescrita.
- **Cache:** Redis gerenciado (Railway Redis add-on ou Upstash).

### 4. Identificador de entidade: `ObjectId` hex → `Guid`

O `User.Id` hoje é uma string no formato ObjectId hex gerada por
`Convert.ToHexString(RandomNumberGenerator.GetBytes(12))`. Isso é um artefato do MongoDB
sem semântica nativa no SQL Server.

**Decisão:** `User.Id` (e demais entidades) migra para `Guid` (`uniqueidentifier`).

Impactos diretos identificados:

- `User.cs`: tipo de `Id` muda de `string` para `Guid`; factory `GenerateObjectId()` é
  substituída por `Guid.NewGuid()`.
- JWT `sub` claim: passa a serializar o `Guid` como string (`Guid.ToString()`).
- `MeController`: o parsing atual `ObjectId.TryParse` vira `Guid.TryParse`.
- Repositórios (`UserRepository`, `RefreshTokenRepository`, etc.): assinaturas de método
  que recebem `string id` passam a receber `Guid id`.
- Testes unitários e de integração: fixtures e asserts que constroem IDs hex são atualizados.

### 5. Migrations versionadas com EF Core

O `MongoMigrationsHostedService` e toda a infraestrutura de migration Mongo
(`MongoMigrationRunner`, `MongoMigrationHistoryRepository`, etc.) são removidos.

**Substitutos:**

- **`AegisIdentity.Migrations`**: passa a conter migrations EF Core geradas via
  `dotnet ef migrations add`. O projeto mantém o `DbContext` como único ponto de verdade
  do schema.
- **Aplicação no startup**: `Database.Migrate()` chamado no startup da `Api` (equivalente
  ao `MongoMigrationsHostedService`). Migrations são aplicadas incrementalmente a cada deploy.
- **`AegisIdentity.Migrations.Cli`**: ferramenta CLI para gerar e aplicar migrations em
  desenvolvimento (`dotnet ef` wrapper, se mantido) ou removido em favor do `dotnet ef`
  direto.

**Dados iniciais (admin e perfis padrão):** inseridos via migration de dados EF Core
(`migrationBuilder.InsertData` ou SQL raw em `Up()`), executada junto com as migrations de
schema. Não existe mecanismo de seed em runtime nem CLI de seed separado. Admin inicial e
perfis padrão chegam ao banco pela migration — esta responsabilidade pertence a INFRA-04 e
AUTH-12.

**Distinção importante:** a reconciliação aditiva de permissions do perfil `Administrator`
no startup (derivada do discovery de endpoints em AUTH-09) **não é seed de dados de negócio**
— é uma rotina de sincronização que lê o sistema e garante que o perfil padrão cobre todas
as permissões registradas. Essa rotina roda no startup independentemente de migrations.

### 6. Soft-delete global

Nenhuma entidade é deletada fisicamente. Toda entidade de domínio carrega:

```csharp
public bool IsDeleted { get; set; }
public DateTime? DeletedAt { get; set; }
```

O EF Core aplica um **global query filter** no `DbContext` para esconder registros deletados
por padrão:

```csharp
modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
```

Para consultas administrativas que precisam ver registros deletados:

```csharp
dbContext.Users.IgnoreQueryFilters().Where(...);
```

**Filtered unique index** para permitir reuso de `Email` e `Username` após soft-delete
(sem violar unicidade):

```csharp
entity.HasIndex(u => u.Email)
      .IsUnique()
      .HasFilter("[IsDeleted] = 0");
```

SQL equivalente:

```sql
CREATE UNIQUE INDEX UX_Users_Email ON Users (Email) WHERE IsDeleted = 0;
```

Sem este filtered index, um soft-delete seguido de novo cadastro com o mesmo email violaria
a constraint unique — comportamento indesejado para um sistema de identidade.

### 7. Cache distribuído

Redis (INFRA-06 / AUTH-11) como cache distribuído da solução. Caso de uso principal:
**cache das permissões efetivas de cada usuário** (chave por usuário), consultado pelo
enforcement de autorização (AUTH-11) e pelo helper de Razor do Backoffice (AUTH-14),
mantendo uma única fonte de verdade coerente entre a API e o Backoffice (e entre múltiplas
instâncias).

Invalidação **imediata por evento de domínio**: quando um perfil, permissão ou associação
muda (AUTH-15), um evento dispara o evict das chaves dos usuários afetados — o TTL serve
apenas como rede de segurança, não como mecanismo primário de frescor.

Estratégia de fallback: quando o Redis estiver indisponível, o enforcement cai de volta ao
banco (consulta das permissões efetivas via repositório) e repopula o cache quando o Redis
voltar. Redis fora do ar nunca derruba nem abre indevidamente a autorização (degradação
graciosa). Detalhes em INFRA-06/AUTH-11.

### 8. Hangfire: provider SQL Server

O `Hangfire.Mongo` é substituído por `Hangfire.SqlServer`. A string de conexão do Hangfire
aponta para o mesmo banco SQL Server da aplicação (schema separado `HangFire` por convenção
do provider).

```csharp
services.AddHangfire(config =>
    config.UseSqlServerStorage(connectionString, new SqlServerStorageOptions { ... }));
```

### 9. Tradução de violação de unique constraint

O MongoDB lança `MongoWriteException` com código `11000` em duplicata. No SQL Server, as
exceções equivalentes chegam via:

```csharp
catch (DbUpdateException ex)
    when (ex.InnerException is SqlException sqlEx
          && sqlEx.Number is 2627 or 2601)
{
    // 2627 = PRIMARY KEY / UNIQUE CONSTRAINT violation
    // 2601 = UNIQUE INDEX violation (filtered unique indexes inclusive)
}
```

Toda lógica de domínio que hoje trata `11000` é atualizada para este padrão.

---

## Consequências

### Positivas

- **Integridade referencial garantida pelo banco:** foreign keys entre User, tokens e futuras
  entidades de autorização eliminam classes inteiras de inconsistência.
- **Migrations versionadas e rastreáveis:** EF Core Migrations com histórico em
  `__EFMigrationsHistory`, rollback possível, tooling maduro.
- **Stack única:** um `DbContext`, um connection string, um health check, uma stack de
  deploy.
- **Hangfire estável:** `Hangfire.SqlServer` é o provider oficial, amplamente testado em
  produção.
- **Soft-delete seguro:** filtered unique index resolve o problema de reuso de email/username
  sem lógica adicional na aplicação.
- **Deploy Railway simplificado:** SQL Server e Redis como add-ons nativos da plataforma.

### Negativas / trade-offs

- **Reescrita de repositórios:** todos os repositórios Mongo são substituídos por
  repositórios EF Core. Trabalho estimado: INFRA-02 (schema + DbContext), INFRA-03
  (repositórios), INFRA-04 (migration inicial + dados).
- **Perda de flexibilidade de schema:** mudanças de schema exigem migration explícita —
  perde-se a agilidade do Mongo para campos ad-hoc. Aceitável dado o estágio do projeto.
- **SQL Server em Docker para dev:** container de ~1.5 GB vs. processo Mongo menor.
  Trade-off aceitável em máquina de dev Windows x64.
- **`Guid` como PK:** levemente maior que int (16 bytes vs. 4), e UUIDs aleatórios causam
  fragmentação de índice. Mitigação possível via `Guid.CreateVersion7()` (sequencial) se
  fragmentação se tornar problema — mas não é premature optimization agora.

### Projetos impactados

| Projeto | Impacto |
|---|---|
| `AegisIdentity.DataAccess` | Substituição completa: MongoDB → EF Core `DbContext` + configurações de entidade |
| `AegisIdentity.Migrations` | Reescrito: migrations EF Core substituem runner Mongo caseiro |
| `AegisIdentity.Migrations.Cli` | Reescrito ou removido (avaliado em INFRA-02) |
| `AegisIdentity.Jobs` | `Hangfire.Mongo` → `Hangfire.SqlServer`; referência a `MongoOptions` removida |
| `AegisIdentity.IntegrationTests` | Connection string e fixtures migram para SQL Server (Testcontainers ou LocalDB) |
| `AegisIdentity.Infrastructure` | `MongoOptions` POCO provavelmente removido ou substituído por `SqlOptions` |
| `AegisIdentity.Api` | Startup: `MongoMigrationsHostedService` → `Database.Migrate()`; `MeController`: `ObjectId.TryParse` → `Guid.TryParse` |
| `AegisIdentity.Backoffice` | Sem impacto direto na camada de apresentação; impactado indiretamente via serviços |
| `AegisIdentity.Domain` (User) | `Id`: `string` ObjectId hex → `Guid`; remoção de `GenerateObjectId()` |

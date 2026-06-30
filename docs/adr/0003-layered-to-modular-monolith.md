# ADR-0003: Migração de Clean Architecture em camadas para monolito modular

**Status:** Accepted — 2026-06-29
**Related:** ADR-0001 (SQL Server + EF Core), épicos E0–E4 do board "Portifolio Projects"

---

## Contexto

O Lumen nasceu organizado por **camadas técnicas** (`Lumen.Domain`, `Application`
[`CommandHandlers`/`ReadModels`/`EventHandlers`], `Infrastructure` [`DataAccess`/`Integration`],
`Presentation`, `Jobs`, `SharedKernel`), com um **único `LumenDbContext`** no schema `dbo`
agregando todas as entidades (Users, Tokens, Permissions, Profiles, UserProfiles, Audit…).

Esse modelo funcionou para o estágio inicial, mas apresentava fricção crescente para os objetivos
do projeto:

- **Capacidades de negócio espalhadas por camada:** a auditoria, por exemplo, tinha entidade no
  Domain, handlers no `EventHandlers` e persistência no `DataAccess` — nenhuma fronteira impedia
  que um fluxo de identidade dependesse diretamente dos internos da auditoria.
- **Acoplamento por um DbContext único:** todas as entidades num só contexto/schema dificultam
  isolar, testar e — um dia — extrair uma capacidade como biblioteca ou serviço.
- **Objetivo declarado:** transformar a infraestrutura de modularidade num building block
  reutilizável (`Lumen.Modularity`) que sirva a outros projetos — "basta usar a annotation
  `[Module]` que funciona".

A janela de migração é agora, com o domínio ainda enxuto (duas capacidades claras: identidade e
auditoria), antes de o custo de reorganizar verticais crescer.

---

## Decisão

**Adotar um monolito modular**: cada capacidade de negócio é uma **vertical autocontida**
(domínio + aplicação CQRS + persistência própria + Contratos públicos + ponto de registro
`[Module]`/`IModule`), com **0 dependências dos internals de outro módulo**. Comunicação
cross-módulo apenas via **Contratos** (interfaces/DTOs/integration events) e um **event bus
in-process**.

Módulos de negócio: **`Identity`** (autenticação + autorização: Users, Tokens, Security, Auth
flows, Profiles, Permissions, UserProfiles, e-mails) e **`Audit`**. `Notifications` (e-mail) e
`Security` (JWT/BCrypt/HIBP) permanecem **internos ao módulo Identity** (ver A2 abaixo).

A migração foi conduzida de forma **incremental com piloto** (E0 → E4): primeiro o building block,
depois o módulo piloto de menor risco (Audit), depois Identity, depois a limpeza do composition
root, depois docs e validação.

---

## Alternativas consideradas

### A1 — Manter a Clean Architecture em camadas

Descartada. Mantinha o acoplamento por DbContext único e não dava fronteiras verificáveis entre
capacidades, indo na direção oposta do objetivo de extrair `Lumen.Modularity` como lib reutilizável.

### A2 — `Security`/`Notifications` como building blocks compartilhados (fora do Identity)

Seriamente considerada — são tecnicamente genéricas. Descartada porque `IJwtService`,
`IPasswordHasher` etc. dependem diretamente da entidade `User` do domínio de identidade; extrair
criaria acoplamento circular (building block dependendo dos Contratos do Identity). Mantê-las
internas ao Identity é mais simples. Reavaliar se um segundo módulo precisar de hash/JWT.

### A3 — Banco físico por módulo / microsserviços

Descartada por ora (excesso para o estágio atual): perde transação local simples e complica
deploy. O isolamento adotado — **schema por módulo no mesmo banco**, sem FK cross-schema — entrega
a fronteira sem o custo operacional.

### A4 — Comunicação só por eventos (sem assembly de Contratos)

Descartada: queries síncronas cross-módulo (ex.: Backoffice reutilizando `IUserPermissionService`)
ficariam tortuosas. Adotado o padrão "Contratos públicos + event bus" (síncrono via interface
pública de Contratos; assíncrono via bus).

---

## Decisão detalhada

### 1. Building block `Lumen.Modularity` (`src/BuildingBlocks/`)

- `[Module]` (`ModuleAttribute`) + `IModule` com `RegisterServices(IServiceCollection, IConfiguration)`
  e `MapEndpoints(IEndpointRouteBuilder)`.
- `AddModules(assemblies…)` / `MapModules()`: auto-discovery por assembly scanning — o host registra
  serviços e mapeia endpoints de todos os módulos sem fiação manual.
- `IEventBus` / `InProcessEventBus` / `AddEventBus(assemblies…)`: publish/subscribe in-process;
  `PublishAsync<TEvent>` abre um **DI scope por publicação** (handlers `Scoped`, como DbContext,
  funcionam sob um bus singleton) e resolve todos os `IIntegrationEventHandler<TEvent>`.
- `IIntegrationEvent` / `IntegrationEvent` (record base com `EventId`/`OccurredOn`).
- **Agnóstico ao Lumen** por design — preparado para virar NuGet.

### 2. Estrutura de um módulo (`src/Modules/<X>/`)

```
Lumen.Modules.<X>.Contracts/    PUBLIC: integration events + interfaces expostas
Lumen.Modules.<X>/              Internals: Domain, Application (CQRS), Infrastructure, Persistence, <X>Module.cs
Lumen.Modules.<X>.Migrations/   EF migrations + <X>MigrationsHostedService
```

### 3. Banco: schema + DbContext + migrations por módulo

- `identity.*` (`IdentityDbContext`) e `audit.*` (`AuditDbContext`), cada um com migrations próprias
  aplicadas no startup por um hosted service.
- **Sem FK cross-schema** — referências cruzadas por `Guid` id (o que torna a extração futura barata).
- O schema legado `dbo.*` (do antigo `LumenDbContext`) fica **órfão**: não é mais escrito; dados
  históricos permanecem intocados. Não há migração de dados — o módulo Identity cria `identity.*`
  com seed completo de permissões/perfis/usuário bootstrap.

### 4. Comunicação cross-módulo

- Síncrona: via interfaces públicas dos Contratos (ex.: `IUserPermissionService` em
  `Identity.Contracts`, reutilizado pelo Backoffice).
- Assíncrona: o Identity publica integration events (`UserLoggedIn`, `UserLockedOut`,
  `ProfilePermissionsSet`, `UserProfileAssigned`, `UserProfileRemoved`, `UserPermissionsChanged`)
  no `IEventBus`; o Audit e o push do grafo ao vivo consomem independentemente. `CleanupJobExecuted`
  pertence aos Contratos do Audit (publicado pelo job de limpeza).

### 5. Composition root

Api e Backoffice compõem os módulos apenas via `AddModules` / `AddEventBus` / `MapModules`. Hangfire
e o `AuthorizationGraphHub` (SignalR) são tratados no host; o handler de push do grafo é um
`IIntegrationEventHandler<UserPermissionsChangedEvent>` registrado via `AddEventBus`.

### 6. Fronteiras verificadas por teste

`tests/Lumen.ArchitectureTests` foi reescrito (NetArchTest) para **7 regras de fronteira de módulo**
— módulo não importa internals de outro; Contratos independentes entre si; SharedKernel e
`Lumen.Modularity` não conhecem módulos de negócio. Violação = build de testes falha.

---

## Consequências

### Positivas

- **Fronteiras reais e verificadas:** isolamento entre capacidades garantido em build, não em code
  review.
- **Building block reutilizável:** `Lumen.Modularity` é plug-and-play e pronto para extração como lib.
- **Caminho barato para extração:** schema próprio + sem FK cross-módulo + Contratos tornam viável
  promover um módulo a serviço no futuro.
- **Verticais coesas:** cada módulo é entendido, testado e evoluído isoladamente.

### Negativas / trade-offs

- **Mais assemblies e cerimônia:** Contratos explícitos e DbContext/migrations por módulo aumentam a
  contagem de projetos.
- **Pendências de transição:** `Lumen.IntegrationTests` ficou fora da solução (precisa reescrita para
  `IdentityDbContext`); projetos legados (`Lumen.DataAccess`/`Integration`/`Migrations`/`Migrations.Cli`)
  permanecem em disco apenas enquanto a integração não é reescrita.
- **`dbo.*` órfão:** decisão conservadora; se houver dados a migrar de `dbo.*` para `identity.*` em
  algum ambiente, é uma operação manual fora do escopo desta migração.

### Projetos impactados

| Projeto | Impacto |
|---|---|
| `Lumen.Modularity` (novo) | Building block de modularidade e event bus |
| `Lumen.Modules.Identity[.Contracts/.Migrations]` (novo) | Vertical de identidade + autorização |
| `Lumen.Modules.Audit[.Contracts/.Migrations]` (novo) | Vertical de auditoria (piloto) |
| `Lumen.CommandHandlers`, `Lumen.ReadModels`, `Lumen.EventHandlers`, `Lumen.Domain` | Removidos (absorvidos pelos módulos) |
| `Lumen.Infrastructure` | Reduzido a `SqlServerOptions` + extensão (mínimo cross-cutting para Hangfire) |
| `Lumen.DataAccess`, `Lumen.Integration`, `Lumen.Migrations`, `Lumen.Migrations.Cli` | Fora da solução; pendentes de remoção após reescrita dos IntegrationTests |
| `Lumen.Api`, `Lumen.Backoffice`, `Lumen.Jobs` | Composition root via auto-discovery dos módulos |
| `Lumen.ArchitectureTests` | Reescrito para 7 regras de fronteira de módulo |

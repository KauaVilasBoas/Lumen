# Lumen

Plataforma de identidade e autorização (.NET 8) construída como **monolito modular** com DDD e CQRS. Expõe uma **API REST** (`Lumen.Api`) para autenticação/identidade e um **Backoffice MVC** (`Lumen.Backoffice`) para administração. Este documento é a fonte de verdade da stack e das convenções arquiteturais — todo agente e contribuição deve se basear aqui.

## Stack

| Área | Tecnologia | Versão |
|------|-----------|--------|
| Runtime / SDK | .NET | `net8.0`, `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true` |
| Gerência de pacotes | Central Package Management | `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`) |
| API / Web | ASP.NET Core (`Microsoft.NET.Sdk.Web`) | 8.0 |
| Mediator / CQRS | MediatR | 12.4.1 |
| Validação | FluentValidation / FluentValidation.AspNetCore | 11.11.0 / 11.3.0 |
| Persistência (escrita) | EF Core + SQL Server | 8.0.15 |
| Persistência (leitura) | EF Core (read-only, `AsNoTracking`) | 8.0.15 |
| AuthN | JWT Bearer + `System.IdentityModel.Tokens.Jwt`; Backoffice usa Cookie Auth | 8.0.15 / 8.9.0 |
| Hashing de senha | BCrypt.Net-Next | 4.0.3 |
| Cache | Memory + StackExchange Redis | 8.0.x |
| E-mail | MailKit (MimeKit transitivo) | 4.16.0 |
| Background jobs | Hangfire (AspNetCore + SqlServer) | 1.8.14 |
| Observabilidade | Serilog (Console, File, Compact, enrichers) | 9.0.0 |
| Docs de API | Swashbuckle (Swagger) | 8.1.1 |
| Rate limiting | Middleware nativo `Microsoft.AspNetCore.RateLimiting` (shared framework, sem pacote) | — |
| Testes | xUnit, NSubstitute, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing, Testcontainers (MsSql + Redis), coverlet | ver `Directory.Packages.props` |

> Versões são centralizadas. **Nunca** fixe versão em `.csproj`; adicione/atualize em `Directory.Packages.props` e referencie sem `Version` no projeto.

## Estrutura da solução (`Lumen.sln`)

```
src/
  BuildingBlocks/
    Lumen.Modularity/             IModule, [Module], AddModules/MapModules, IEventBus/InProcessEventBus/AddEventBus, IIntegrationEvent
  SharedKernel/
    Lumen.SharedKernel/           Constants, Exceptions, Util (cross-cutting, sem dependências de infra ou módulos)
  Modules/
    Identity/
      Lumen.Modules.Identity.Contracts/   Contratos públicos: 6 integration events + IUserPermissionService
      Lumen.Modules.Identity/             Módulo vertical: domínio, CQRS, infra, IdentityDbContext (schema identity.*)
      Lumen.Modules.Identity.Migrations/  EF migrations do módulo (IdentityMigrationsHostedService)
    Audit/
      Lumen.Modules.Audit.Contracts/      Contratos públicos: CleanupJobExecutedEvent (+ events do Identity que o Audit consome)
      Lumen.Modules.Audit/                Módulo vertical: AuditEntry, AuditDbContext (schema audit.*), event handlers
      Lumen.Modules.Audit.Migrations/     EF migrations do módulo (AuditMigrationsHostedService)
  Lumen.Infrastructure/           SqlServerOptions + InfrastructureOptionsExtensions (mínimo cross-cutting)
  Jobs/
    Lumen.Jobs/                   Hangfire (registro de jobs e dashboard)
  Presentation/
    Lumen.Backoffice/             Backoffice MVC (Controllers, Views, ViewComponents)
  Lumen.Api/                      Host da API REST (Controllers, Program.cs, DI root)
tests/
  Lumen.UnitTests/                Testes legados não migrados (ViewComponents, helpers, queries de auditoria)
  Lumen.Modules.Identity.Tests/   Handler + validator tests do módulo Identity (xUnit + NSubstitute + FluentAssertions)
  Lumen.Modules.Audit.Tests/      Testes de domínio do módulo Audit
  Lumen.Modularity.UnitTests/     Testes do building block Lumen.Modularity
  Lumen.ArchitectureTests/        Testes de fronteira de módulo (NetArchTest.Rules — 7 regras)
```

> `tests/Lumen.IntegrationTests` foi removida temporariamente da solução — exige reescrita para usar `IdentityDbContext` em vez do legado `LumenDbContext`.

> **Em andamento — autorização como biblioteca plugável ([ADR-0004](docs/adr/0004-authorization-as-library.md)):** a capacidade de autorização (Permissions, Profiles, UserProfiles, `IUserPermissionService`, enforcement, discovery, backoffice) será extraída do módulo Identity para a família de pacotes `Lumen.Authorization*` — núcleo agnóstico de ASP.NET (`Lumen.Authorization`), migrations (`.Migrations`), cola web com `[RequirePermission]`/policy provider (`.AspNetCore`) e backoffice montável como Razor Class Library (`.Backoffice`). Alvo: qualquer app ASP.NET Core faz `AddLumenAuthorization(connectionString)` (auto-migração do schema `Lumen`, tabelas singulares) + `AddLumenAuthorizationEnforcement()` + `[RequirePermission]` (declara e enforça) + `MapLumenBackoffice("/lumen")`, trazendo o próprio login. Épico "Lumen Authz Lib" (cards LIB-00…LIB-14).
>
> **LIB-07 (concluído):** `Lumen.Authorization.AspNetCore` criado com `PermissionPolicyProvider`, `PermissionRequirement`, `PermissionAuthorizationHandler`, `RequirePermissionAttribute`, `PermissionGroupAttribute`, `ControllerNameNormalizer`. Máquina de enforcement movida do host; SharedKernel limpo dos atributos. Registro: `AddLumenAuthorizationEnforcement()`.
>
> **LIB-06 (concluído):** `RequirePermissionAttribute` implementa `IAuthorizationRequirementData` — atributo único declara E enforça. Code explícito ou convenção `controller.action` via `ControllerActionDescriptor` do endpoint. Controllers do host migrados para `[RequirePermission(PermissionCodes.X)]` sem `[Authorize]` redundante. Policy nomeada aceita formato `code.com.ponto` (compat) ou `Lumen:code`.
>
> **LIB-08 (concluído):** `LumenAuthorizationOptions.UserIdClaimType` (default `ClaimTypes.NameIdentifier`) torna o claim de userId configurável. `IUserIdAccessor` (em `Lumen.Authorization.Contracts`) abstrai a leitura do userId — implementação default `ClaimsUserIdAccessor` em `Lumen.Authorization.AspNetCore`, registrada via `TryAddSingleton` em `AddLumenAuthorizationEnforcement()`. `PermissionAuthorizationHandler` delega a extração ao `IUserIdAccessor`.
>
> **LIB-09 (concluído):** `PermissionDiscoveryScanner` e `PermissionDiscoveryAndReconciliationHostedService` movidos para `Lumen.Authorization.AspNetCore`. Hosted service unificado executa `descobrir → SyncDiscoveredAsync → ReconcileAdministratorAsync` em sequência usando `IPermissionSyncService` diretamente. Registro: `AddLumenAuthorizationDiscovery()`. Todos os wrappers intermediários e o record `DiscoveredPermission` do host foram removidos; `Lumen.Api/Program.cs` usa `AddLumenAuthorizationDiscovery()` no lugar de `AddPermissionDiscovery()`.

## Arquitetura modular

### Building block: `Lumen.Modularity`

Lib reutilizável que viabiliza o plug-and-play dos módulos nos hosts:

- **`[Module]` / `IModule`**: a annotation `[Module]` marca a classe principal de cada módulo. `IModule` exige dois métodos: `RegisterServices(IServiceCollection, IConfiguration)` e `MapEndpoints(IEndpointRouteBuilder)`.
- **`AddModules(assemblies...)`**: extension method que faz auto-discovery de todos os `[Module]` nos assemblies fornecidos e chama `RegisterServices` em cada um.
- **`MapModules()`**: extension method que resolve os módulos do container e chama `MapEndpoints` em cada um.
- **`IEventBus` / `InProcessEventBus`**: barramento in-process; `PublishAsync<TEvent>` cria um scope DI, resolve todos os `IIntegrationEventHandler<TEvent>` e os invoca em sequência.
- **`AddEventBus(assemblies...)`**: registra `InProcessEventBus` como singleton e descobre/registra automaticamente todos os `IIntegrationEventHandler<TEvent>` nos assemblies fornecidos como `Scoped`.
- **`IIntegrationEvent` / `IntegrationEvent`**: base para eventos de integração cross-módulo; transportam `EventId` (Guid) e `OccurredOn` (DateTimeOffset UTC).

### Estrutura interna de um módulo

Cada módulo é uma vertical autocontida com suas camadas internas. Exemplo (Identity):

```
src/Modules/Identity/
  Lumen.Modules.Identity.Contracts/   ← PUBLIC: integration events + interfaces expostas a outros módulos/hosts
  Lumen.Modules.Identity/
    Domain/                            ← Entidades, repositórios (interfaces), domain services — todos internal
    Application/                       ← Command/QueryHandlers, Behaviors, EventHandlers — todos internal
    Infrastructure/                    ← JWT, BCrypt, Redis cache, MailKit, HIBP — todos internal
    Persistence/                       ← IdentityDbContext, Repositories, Configurations — todos internal
    IdentityModule.cs                  ← [Module] IModule — entry point de DI e endpoints
  Lumen.Modules.Identity.Migrations/  ← EF migrations + IdentityMigrationsHostedService
```

### Regra de fronteira de módulo (OBRIGATÓRIA)

**0 dependências de internals entre módulos.** Cross-module só via Contratos + event bus:

- Um módulo **nunca** importa namespace interno de outro módulo (`Lumen.Modules.Identity.*` ≠ `Lumen.Modules.Audit.*`).
- Comunicação cross-módulo usa apenas: `Lumen.Modules.<X>.Contracts` + `IEventBus.PublishAsync`.
- Os assemblies de Contratos (`*.Contracts`) só podem depender de `Lumen.Modularity` (para herdar `IntegrationEvent`).
- Os Contratos de um módulo não importam Contratos de outro módulo.

### Schema e DbContext por módulo

Cada módulo gerencia seu próprio schema SQL:

- `identity.*` — `IdentityDbContext` do módulo Identity (Users, RefreshTokens, Tokens, Profiles, Permissions, UserProfiles, GroupPermissions, PermissionProfiles).
- `audit.*` — `AuditDbContext` do módulo Audit (AuditEntries).
- Sem FKs cross-schema. Referências cruzadas são por ID (Guid), sem `FOREIGN KEY` declarado.
- Migrations ficam em `Lumen.Modules.<X>.Migrations` e são aplicadas por um `HostedService` na inicialização (`IdentityMigrationsHostedService`, `AuditMigrationsHostedService`).

### Hosts (como os módulos são compostos)

`Lumen.Api/Program.cs`:
```csharp
builder.Services.AddModules(IdentityModule.Assembly, AuditModule.Assembly);
builder.Services.AddEventBus(IdentityModule.Assembly, AuditModule.Assembly, typeof(Program).Assembly);
// ...
app.MapModules();
```

`Lumen.Backoffice/Program.cs`:
```csharp
builder.Services.AddModules(IdentityModule.Assembly); // só IUserPermissionService via Redis
```

## Regras arquiteturais (OBRIGATÓRIAS)

### CQRS — Command (escrita)
- **Command e CommandHandler vivem no MESMO arquivo `.cs`.** O `record` do Command pode estar fora da classe do Handler, mas no mesmo arquivo. Padrão atual: `Command` aninhado como `public sealed record Command(...) : IRequest<Unit>` dentro da classe do Handler.
- **O Controller dispara o Command via MediatR** (`_mediator.Send(command, ct)`). Controller não contém regra de negócio.
- **Dentro de um CommandHandler só é permitido usar repositórios** (escrita via EF Core). Um CommandHandler **nunca** chama um QueryHandler nem acessa `IEventBus` fora da camada de Aplicação do próprio módulo (handlers de comandos que precisam publicar eventos usam `IEventBus` diretamente).
- Validação via `AbstractValidator<Command>` no mesmo arquivo, aplicada pelo `ValidationBehavior` do MediatR.

### CQRS — Query (leitura)
- **Query e QueryHandler vivem no MESMO arquivo `.cs`** (mesmo padrão do Command).
- **O QueryHandler usa EF Core para leitura**, preferindo `AsNoTracking`. Pode acessar o `DbContext`/repositórios de leitura; corrija N+1 com projeção/joins/`Include`.
- Query handlers são **leitura pura**: sem `Insert/Update`, sem domain events.

### Apresentação (Backoffice MVC — telas)
- **Fluxos de tela usam sempre ViewComponent + ViewModel.** Nada de montagem de tela/regra na View ou no Controller.
- **Telas "burras" (sem lógica) usam PartialView.**
- **Cada componente de tela é um ViewComponent separado.** Separe responsabilidades visuais em ViewComponents distintos; não concentre tudo em um só.

## Convenções de código

- **Código self-documenting, SEM comentários.** Nomes de classes/métodos devem comunicar a intenção. (Comentários explicativos existentes no projeto são legado; não os replique em código novo.)
- **Nada de valores hardcoded.** Todo literal (mensagens, assuntos de e-mail, limites, chaves, templates) vem de `SharedKernel/Constants` (ex.: `AuthErrorMessages`, `EmailSubjects`, `ValidationLimits`, `EmailTemplateNames`, `TokenSizes`). Se não existir, crie a constante lá.
- **Permissões são apenas no banco.** Permissões curadas são semeadas via migration EF — nunca códigos de permissão inline.
- **Segredos** via User Secrets / variáveis de ambiente — nunca no repositório.
- `Nullable` e `TreatWarningsAsErrors` estão ligados: warning quebra o build. Trate-os.

## Segurança (padrões já estabelecidos)

- Tokens (reset/confirmação) são guardados e consultados por **hash SHA-256**; o token bruto nunca é persistido. Geração via `RandomNumberGenerator` + base64url.
- Senhas via BCrypt. Troca/reset de senha **revoga todos os refresh tokens ativos**.
- Endpoints autenticados usam a `FallbackPolicy` (`RequireAuthenticatedUser`); o `userId` vem do claim `sub` do JWT (`Guid.TryParse`), nunca de input do cliente.
- Fluxos de e-mail (forgot/resend) respondem de forma uniforme para evitar enumeração de conta.

## Git e processo

- **Nunca commitar direto na `main`.** Sempre crie uma branch a partir da `main`.
- **Commits atômicos**, pequenos e de propósito único, em **Conventional Commits**. Separe `feat` / `test` / `docs` / `refactor` em commits distintos.
- **Nunca** adicionar trailer `Co-Authored-By` de IA nos commits.
- **Atualize o `CHANGELOG.md`** seguindo o padrão dos commits `docs(changelog)` existentes.
- Não faça push nem abra PR sem ser solicitado. `api.github.com` está bloqueada no ambiente: abra PR pelo navegador via link `pull/new`, com o corpo pronto.

## Testes

- **Unit / Module** (`Lumen.Modules.Identity.Tests`, `Lumen.Modules.Audit.Tests`, `Lumen.UnitTests`): handlers e validators com xUnit + NSubstitute + FluentAssertions. Toda nova feature deve ter testes de handler e de validator. O módulo Identity expõe internals via `InternalsVisibleTo` para o projeto de testes.
- **Integration** (`Lumen.IntegrationTests`): endpoints via `WebApplicationFactory` + Testcontainers (SQL Server + Redis). **Exigem Docker rodando**; rodam no CI. Quando não executados localmente, declare isso explicitamente. **Atualmente removida da solution** — exige reescrita para usar `IdentityDbContext`.
- **Architecture** (`Lumen.ArchitectureTests`): testes de fronteira de módulo via NetArchTest.Rules. Devem ser rodados junto com os unit tests. **Não exigem Docker.**

## Constraints de arquitetura (testes automatizados)

Projeto `tests/Lumen.ArchitectureTests` — rodar com `dotnet test tests/Lumen.ArchitectureTests`.
Cada regra abaixo tem um teste correspondente em `ArchitectureTests.cs`.

| # | Regra | Falha quando |
|---|-------|-------------|
| 01 | SharedKernel não depende de módulos ou Modularity | Tipo em `Lumen.SharedKernel` importa `Lumen.Modules.Identity`, `Lumen.Modules.Audit` ou `Lumen.Modularity` |
| 02 | Lumen.Modularity não depende de módulos de negócio | Tipo em `Lumen.Modularity` importa `Lumen.Modules.Identity`, `Lumen.Modules.Audit` ou `Lumen.SharedKernel` |
| 03 | Identity module não referencia internals do Audit | Tipo em `Lumen.Modules.Identity` importa namespace `Lumen.Modules.Audit` |
| 04 | Audit.Contracts não referencia internals do Identity | Tipo em `Lumen.Modules.Audit.Contracts` importa namespace `Lumen.Modules.Identity` |
| 05 | Identity.Contracts não referencia internals do Audit | Tipo em `Lumen.Modules.Identity.Contracts` importa namespace `Lumen.Modules.Audit` |
| 06 | Identity.Contracts não referencia Audit.Contracts | Tipo em `Lumen.Modules.Identity.Contracts` importa `Lumen.Modules.Audit.Contracts` |
| 07 | Audit.Contracts não referencia Identity.Contracts | Tipo em `Lumen.Modules.Audit.Contracts` importa `Lumen.Modules.Identity.Contracts` |

> Violação detectada = **build de testes falha**. Corrija a dependência, não o teste.

## Comandos

```bash
dotnet build Lumen.sln                                    # build (warnings = erro)
dotnet test tests/Lumen.Modules.Identity.Tests            # testes do módulo Identity
dotnet test tests/Lumen.Modules.Audit.Tests               # testes do módulo Audit
dotnet test tests/Lumen.Modularity.UnitTests              # testes do building block
dotnet test tests/Lumen.ArchitectureTests                 # constraints de fronteira
dotnet test tests/Lumen.UnitTests                         # testes legados (ViewComponents, etc.)
# IntegrationTests exigem Docker e estão fora da solution — pendente reescrita para IdentityDbContext

dotnet ef migrations add <Nome> \
  -p src/Modules/Identity/Lumen.Modules.Identity.Migrations \
  -s src/Modules/Identity/Lumen.Modules.Identity.Migrations    # nova migration do Identity

dotnet ef migrations add <Nome> \
  -p src/Modules/Audit/Lumen.Modules.Audit.Migrations \
  -s src/Modules/Audit/Lumen.Modules.Audit.Migrations          # nova migration do Audit

dotnet ef migrations add <Nome> \
  -p src/Lumen.Authorization.Migrations \
  -s src/Lumen.Authorization.Migrations                        # nova migration do schema Lumen (lib de authz)
```

> Decisões arquiteturais não cobertas aqui são definidas durante o desenvolvimento. Ao encontrar um caso novo, proponha uma abordagem com o trade-off (ganhos/perdas), confirme, e então atualize este documento.

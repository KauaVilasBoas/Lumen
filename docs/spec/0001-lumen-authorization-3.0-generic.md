# SPEC-0001 — Lumen.Authorization 3.0: biblioteca de autorização genérica

**Status:** Draft — aguardando aprovação
**Data:** 2026-07-14
**Autor:** Kauã Vilas Boas (via SDD)
**Relacionados:** ADR-0004 (authorization as library), ADR-0005 (multi-provider), ADR-0006 (tenant-scoped). Este spec **supersede** partes do ADR-0004 (remove discovery/sync/reconciliação e o orquestrador de startup).

---

## 1. North Star

O Lumen é uma **biblioteca genérica de autorização** para apps ASP.NET Core. Instalou:

1. Ganha o atributo `[RequirePermission("...")]`, que faz o **SELECT no banco** e barra a requisição **antes** de cair no endpoint.
2. O Lumen **cria o schema `Lumen` e suas tabelas** via migration (automático por padrão; se o consumidor preferir, ele cria o schema e o Lumen reconhece).
3. **A partir daí, toda a população de dados é da aplicação** — permissões, grupos, perfis e atribuições são criados pela app (via migration própria) e geridos em runtime pelo `/lumen`.

**O Lumen NUNCA tem código específico de aplicação.** É uma lib que só auxilia no controle de permissões e perfis dos usuários de cada aplicação.

---

## 2. Princípios invioláveis

1. **Zero auto-população.** O Lumen não semeia permissões, grupos nem perfis. Não há discovery que cria/atualiza catálogo. A única coisa que a migration inicial cria é o **schema e as tabelas vazias**.
2. **Zero enforcement global.** O Lumen só barra onde há `[RequirePermission]`. Endpoints sem o atributo seguem a política da própria app (`[Authorize]`, `[AllowAnonymous]`, `FallbackPolicy` do host). A lib nunca registra um bloqueio global.
3. **Zero acoplamento de identidade.** O Lumen não conhece usuários. Lê o `userId` de um claim configurável. Não há tabela de `Users` no schema `Lumen`.
4. **Zero feature específica de consumidor.** Nada no código do Lumen referencia SISLAB ou qualquer app. Tudo que é "de aplicação" mora na aplicação.

---

## 3. Decisões travadas (Q&A SDD)

| # | Tema | Decisão |
|---|------|---------|
| 1 | Escopo | Remover toda a máquina automática: discovery scanner, catalog sync, reconciliação de admin, validação de boot e o orquestrador `LumenAuthorizationStartupService`. |
| 2 | População | A app popula via **migration própria** + helper SQL opcional (`SeedLumenPermission*`). |
| 3 | Perfis de sistema | **Não semear nada.** Tabelas nascem vazias. |
| 4 | Auto-migração | **Ligada por default**, com flag para desligar; tolera schema/tabelas já existentes. |
| 5 | "Grupo" | Agrupamento de **permissões** (organização/UI). Nunca concede acesso. |
| 6 | Concessão | Só `User → Perfis → Permissões`. Grupos não entram no cálculo. |
| 7 | Resolução do code | **Ambos:** code explícito `[RequirePermission("X.Y")]` **e** convenção `controller.action` quando omitido. |
| 8 | Identidade | **Sem tabela de Users.** `userId` = Guid opaco de um claim (default `NameIdentifier`). `[Authorize]` garante authN. |
| 9 | Fallback | Lumen **não** impõe fallback global. |
| 10 | `/lumen` | Gestão **completa** em runtime: CRUD de perfis, permissões, grupos e atribuições. |
| 11 | Auth do `/lumen` | authN é da app; cada ação do `/lumen` é gateada por `[RequirePermission]` com **codes próprios do Lumen** (constantes públicas), que **a app semeia e atribui**. Lumen não traz login. |
| 12 | Multi-banco | SQL Server + PostgreSQL nesta iteração; outros no roadmap. |
| 13 | Versão | **Authorization 3.0.0** (major, breaking, guia 2.x → 3.0). |
| 14 | Mecânica de schema | EF Migrations; auto por default; se off, `dotnet ef database update` ou script SQL idempotente; tolera schema existente. |
| 15 | Sessão anterior | Revertida por completo — recomeço limpo pela spec. |
| 16 | Helper de seed | Manter `SeedLumenPermission*` (corrigidos) nos pacotes de migrations. |
| 17 | Tenancy | Manter `UserProfile.ScopeId` + `ITenantScopeAccessor` como recurso genérico opcional. |
| 18 | Cache | Cache das permissões efetivas com invalidação (memory/Redis). |
| 19 | Schema | Limpar colunas mortas do discovery: remover `IsOrphan`, `OrphanedAt`, `Controller`, `Action`. |
| 20 | API programática | Manter `IUserPermissionService` para checagem em código. |

---

## 4. Arquitetura de pacotes (pós-limpeza)

| Pacote | Responsabilidade | Muda? |
|--------|------------------|-------|
| `Lumen.Authorization` | Núcleo agnóstico de ASP.NET: entities, `LumenAuthorizationDbContext`, repositórios, `IUserPermissionService`, cache, CQRS de perfis/permissões/grupos/atribuições. | **Enxuga:** sai `IPermissionSyncService`, `PermissionSyncService`, `PermissionCatalogMode`, options de catálogo. |
| `Lumen.Authorization.Contracts` | Contratos públicos: `IUserPermissionService`, `IUserIdAccessor`, `IUserDirectory`, `ITenantScopeAccessor`, eventos. | Mantém. |
| `Lumen.Authorization.Migrations` | Migrations do schema `Lumen` (SQL Server) + hosted service **enxuto** (só aplica migrations) + helpers `SeedLumenPermission*`. | **Ajusta.** |
| `Lumen.Authorization.Migrations.PostgreSQL` | Idem, PostgreSQL. | **Ajusta.** |
| `Lumen.Authorization.AspNetCore` | `[RequirePermission]`, `PermissionPolicyProvider`, `PermissionRequirement`/`Handler`, `ControllerNameNormalizer`, `IUserIdAccessor`, `AddLumenAuthorization` (umbrella). | **Enxuga:** sai discovery, sync, reconcile, validação, `LumenAuthorizationStartupService`. |
| `Lumen.Authorization.Backoffice` | RCL `/lumen`: CRUD de perfis/permissões/grupos/atribuições. Constantes públicas de permissão do backoffice. | **Ajusta:** gate por `[RequirePermission]`. |

---

## 5. Modelo de dados (schema `Lumen`, pós-limpeza)

Sem FKs cross-schema. `UserId`/`ScopeId` são Guids opacos (sem FK para tabelas da app).

- **Permission** — `Id`, `Code` (único por não-deletado), `DisplayName`, `GroupPermissionId?`, `IsDeleted`, `DeletedAt`.
  _Removidos:_ `IsOrphan`, `OrphanedAt`, `Controller`, `Action`.
- **PermissionGroup** — `Id`, `Name` (único), `Description`, `IsDeleted`, `DeletedAt`.
- **Profile** — `Id`, `Name` (único), `Description`, `IsSystem`, `IsDeleted`, `DeletedAt`.
  _`IsSystem`_ permanece como flag genérico: a app pode marcar um perfil como não-deletável (ex.: "Administrador") na migration dela. O Lumen não seta isso sozinho.
- **PermissionProfile** — `Id`, `PermissionId`, `ProfileId`, `IsDeleted`, `DeletedAt`. (grant: perfil → permissão)
- **UserProfile** — `Id`, `UserId`, `ProfileId`, `ScopeId?`, `IsDeleted`, `DeletedAt`. (grant: usuário → perfil, global ou por escopo)

**Cálculo de acesso** (o que o `[RequirePermission]` avalia):
> `userId` tem o code `C` ⇔ existe `UserProfile(userId, profileId, scope)` ativo **e** `PermissionProfile(permissionId, profileId)` ativo **e** `Permission(code = C)` ativo. Escopo respeitado pelo `ITenantScopeAccessor` quando fornecido; sem escopo, atribuições globais valem em qualquer contexto.

---

## 6. Contrato do consumidor (experiência-alvo)

### 6.1 Registro (uma linha)

```csharp
builder.Services.AddLumenAuthorization(connectionString, options =>
{
    options.Provider = DatabaseProvider.PostgreSQL;  // ou SqlServer
    // options.ApplyMigrationsOnStartup = false;      // se a app gerencia o schema
    // options.UserIdClaimType = "sub";               // se o userId não é NameIdentifier
    // options.RedisConnectionString = "...";          // cache distribuído (senão, memory)
});

app.MapLumenBackoffice("/lumen");   // opcional
```

Isso registra: núcleo + enforcement (`[RequirePermission]`) + hosted service que **só aplica as migrations** do schema `Lumen` (quando `ApplyMigrationsOnStartup = true`).

### 6.2 Proteger endpoints

```csharp
[Authorize]                                    // authN: da própria app
public class EstoqueController : ControllerBase
{
    [HttpPost("baixa")]
    [RequirePermission("Estoque.Baixa")]       // authZ: SELECT no banco antes do endpoint
    public IActionResult Baixa() => Ok();

    [HttpGet]
    [RequirePermission]                          // convenção: exige o code "Estoque.Get" (controller.action)
    public IActionResult Get() => Ok();
}
```

- Sem `[RequirePermission]` → o Lumen não interfere.
- `userId` ausente/anônimo → 401. Autenticado sem a permissão → 403.
- Code inexistente no catálogo → o usuário simplesmente não o possui → 403.

### 6.3 Popular o catálogo (migration da app)

A app cria a migration **dela** e usa os helpers (opcionais):

```csharp
protected override void Up(MigrationBuilder mb)
{
    mb.SeedLumenPermissionGroup(name: "Estoque", description: "Gestão de estoque");
    mb.SeedLumenPermission(code: "Estoque.Baixa", displayName: "Registrar baixa", groupName: "Estoque");

    // Codes do próprio backoffice do Lumen — a app decide quem administra o /lumen:
    mb.SeedLumenPermission(code: LumenBackofficePermissions.ProfilesManage, displayName: "Gerenciar perfis");
    // ... e atribui esses codes ao perfil administrador da app.
}
```

> O helper apenas emite `INSERT` idempotente contra `Lumen.Permission`/`Lumen.PermissionGroup`. Funciona dentro de **qualquer** migration da app. Requisito: rodar **depois** que o schema `Lumen` existe (auto-migrado no boot, ou aplicado no pipeline antes desta migration).

### 6.4 Proteger o `/lumen`

As ações do backoffice usam `[RequirePermission(LumenBackofficePermissions.X)]`. A app semeia esses codes (6.3) e os atribui ao seu perfil de administrador. Sem essa atribuição, ninguém acessa a gestão — **a app decide quem administra**.

Constantes públicas expostas pelo Lumen (granularidade `View`/`Manage` por área):

```csharp
public static class LumenBackofficePermissions
{
    public const string ProfilesView       = "Lumen.Profiles.View";
    public const string ProfilesManage     = "Lumen.Profiles.Manage";
    public const string PermissionsView    = "Lumen.Permissions.View";
    public const string PermissionsManage  = "Lumen.Permissions.Manage";
    public const string GroupsManage       = "Lumen.Groups.Manage";
    public const string UserProfilesManage = "Lumen.UserProfiles.Manage";
}
```

`View` = ler/listar; `Manage` = criar/editar/excluir/atribuir.

---

## 7. O que é REMOVIDO no 3.0

| Item | Local | Motivo |
|------|-------|--------|
| `LumenAuthorizationStartupService` | AspNetCore | Orquestrador de catálogo — vira hosted service que só migra. |
| `PermissionDiscoveryScanner` | AspNetCore | Discovery some. |
| `PermissionDiscoveryAndReconciliationHostedService` | AspNetCore | Discovery/reconcile some. |
| `PermissionCatalogValidationService` | AspNetCore | Validação de boot some. |
| `IPermissionSyncService` / `PermissionSyncService` | Núcleo | Sync + reconcile some. |
| `PermissionCatalogMode` (enum) | Núcleo | Modo de catálogo some. |
| Options `CatalogMode`, `FailFastOnMissingPermission`, `AutoGrantAllToAdministrator` | Núcleo | Amarradas ao catálogo automático. |
| DI `AddLumenAuthorizationDiscovery`, `AddLumenAuthorizationStartup` + `[Obsolete]` correlatos | AspNetCore | Superfície de discovery. |
| Colunas `IsOrphan`, `OrphanedAt`, `Controller`, `Action` | schema `Lumen.Permission` | Mortas sem discovery. |

**Mantidos** (enforcement + gestão): `RequirePermissionAttribute`, `PermissionPolicyProvider`, `PermissionRequirement`, `PermissionAuthorizationHandler`, `ControllerNameNormalizer`, `IUserIdAccessor`/`ClaimsUserIdAccessor`, `IUserPermissionService`/cache, `IUserDirectory` (alimenta a lista de usuários do `/lumen` sem o Lumen possuir Users), `ITenantScopeAccessor`, todo o CQRS de perfis/permissões/grupos/atribuições, o backoffice.

**`GetAuthorizationGraphQuery`** (leitura da grade perfis×permissões×usuários) é **mantida no núcleo**, mas perde o campo `IsOrphan` do resultado. É consumida pelo host `Lumen.Api` (controller + hub SignalR) e pelo backoffice MVC de referência — ambos ajustados junto.

---

## 8. Versionamento e compatibilidade

- **Authorization 3.0.0** (e pacotes de migrations 3.0.0, alinhando a família).
- **Breaking changes** documentadas no CHANGELOG + guia 2.x → 3.0.
- **Migrations regeneradas do zero.** Como **nenhum consumidor está em produção** (o SISLAB ainda não foi ao ar), o 3.0 **regenera a migration inicial limpa** por provider (schema `Lumen` já sem as colunas mortas), em vez de manter uma ALTER incremental carregando o passado. É o caminho mais limpo a longo prazo para um major pré-lançamento.
- **Consumidores em dev (ex.: SISLAB):** recriam o schema `Lumen` (drop & re-migrate). Nenhuma perda relevante — são dados de desenvolvimento e o app não está no ar.

---

## 9. Guia de migração do SISLAB (2.x → 3.0)

1. Subir os pacotes `Lumen.Authorization*` para 3.0.0.
2. Simplificar o `AddLumenAuthorization`: remover `CatalogMode`/`FailFastOnMissingPermission`/`AutoGrantAllToAdministrator` (não existem mais).
3. **Aposentar** `LumenPermissionCatalogSeeder` (hosted service de raw-SQL) e o catálogo anti-drift `PermissionDisplayNames` + testes correlatos — eram um contrapeso ao overwrite que não existe mais.
4. Criar uma **migration própria** do SISLAB que usa `SeedLumenPermissionGroup`/`SeedLumenPermission` para semear o catálogo (inclui os codes do `/lumen`), aplicada **depois** do schema `Lumen`.
5. Semear e atribuir `LumenBackofficePermissions.*` ao perfil administrador do SISLAB.

---

## 10. Plano de implementação (commits atômicos)

1. `refactor(authz)`: domínio + configuração + migration incremental que dropa colunas mortas (`IsOrphan`/`OrphanedAt`/`Controller`/`Action`), SQL Server e PostgreSQL.
2. `refactor(authz)`: remover discovery/sync/reconcile/validação/orquestrador + options de catálogo + DI obsoleta.
3. `feat(authz)`: hosted service enxuto (só migrations) + `AddLumenAuthorization` umbrella simplificado (núcleo + enforcement + migrations).
4. `fix(authz-migrations)`: corrigir `SeedLumenPermission*` (tabelas/colunas certas, sem split controller/action) + **testes** (`Lumen.Authorization.Migrations.Tests`).
5. `feat(authz-backoffice)`: `LumenBackofficePermissions` (constantes públicas) + gate das ações do `/lumen`.
6. `docs`: CHANGELOG 3.0.0 + ADR-0007 + guia SISLAB; bumps de versão; atualizar CLAUDE.md e ArchitectureTests.

Cada fase: build limpo (warnings = erro) + testes de handler/validator/enforcement verdes.

---

## 11. Pontos resolvidos (eram abertos)

1. **Granularidade dos codes do backoffice** → **granulado** `View`/`Manage` por área (ver §6.4).
2. **`GetAuthorizationGraphQuery`** → **manter no núcleo**, removendo o campo `IsOrphan`; ajustar host + backoffice MVC de referência.
3. **Convenção `controller.action`** → **PascalCase, sem sufixo `Controller`**: `Estoque.Baixa`.
4. **Colunas `Controller`/`Action`** → **remover**. Confirmado em código: só eram usadas por `PermissionSyncService`/`IPermissionSyncService` (removidos) e pela factory `Permission.Create` (ganha assinatura baseada em `code`). Nenhuma tela depende delas, e o app não foi ao ar.

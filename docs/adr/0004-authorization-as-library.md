# ADR-0004: Extrair a autorização do módulo Identity para uma biblioteca plugável

**Status:** Accepted — 2026-07-01
**Related:** ADR-0003 (monolito modular), épico "Lumen Authz Lib" (cards LIB-00…LIB-14 do board "Portifolio Projects")

---

## Contexto

A ADR-0003 consolidou o Lumen como monolito modular e deixou **autenticação e
autorização juntas dentro do módulo `Identity`** (Users, Tokens, Security, Auth flows,
Profiles, Permissions, UserProfiles, e-mails), tudo no `IdentityDbContext`/schema
`identity.*`. Foi a decisão certa para o estágio anterior.

O objetivo agora é diferente: **entregar a autorização como biblioteca reutilizável**,
instalável por qualquer app ASP.NET Core de terceiros. A experiência-alvo do consumidor:

1. Instala o pacote e chama `AddLumenAuthorization(connectionString)`.
2. No startup, a lib **cria automaticamente** o schema `Lumen` (tabelas `Permission`,
   `PermissionGroup`, `Profile`, `PermissionProfile`, `UserProfile`) via migration.
3. Protege endpoints com **um único atributo** `[RequirePermission("...")]`.
4. Monta a tela de gestão via `MapLumenBackoffice("/lumen")`.
5. **Traz o próprio login** — a lib não autentica; só precisa que o consumidor exponha o
   `userId` num claim.

O código atual já entrega ~70% disso, mas com dois bloqueios estruturais: (a) a autorização
está **acoplada** ao módulo Identity, arrastando JWT/e-mail/BCrypt/HIBP/Redis; (b) a máquina
de enforcement (policy provider, handler, discovery) mora no **host `Lumen.Api`**, não num
pacote empacotável. Este ADR fixa a **fronteira da extração** — o que sai do Identity, o que
fica, e o contrato com o consumidor — para destravar os cards LIB-02…LIB-14.

---

## Decisão

**Extrair a capacidade de autorização do módulo Identity para uma família de pacotes
`Lumen.Authorization*`**, com dependência de ASP.NET isolada num pacote separado e um
`DbContext`/schema próprios. O módulo `Identity` (autenticação) passa a **consumir** a lib;
o consumidor externo pode usar a lib **sem** o módulo Identity.

Arquitetura-alvo de pacotes:

```
Lumen.Authorization              núcleo agnóstico de ASP.NET: entities, LumenAuthorizationDbContext,
                                 repositórios, IUserPermissionService, cache, PermissionSyncService,
                                 CQRS de perfis/permissões
Lumen.Authorization.Migrations   EF migrations do schema Lumen + hosted service de auto-migração
Lumen.Authorization.AspNetCore   [RequirePermission] (declara E enforça), PermissionPolicyProvider,
                                 PermissionRequirement/Handler, discovery, AddLumenAuthorization
Lumen.Authorization.Backoffice   Razor Class Library montável (MapLumenBackoffice)
```

A separação **núcleo vs `.AspNetCore`** é deliberada: o núcleo não conhece
`Microsoft.AspNetCore.*`, mantendo o domínio de autorização testável e reutilizável fora de
um pipeline web.

---

## Fronteira: o que migra, o que fica

### Migra para `Lumen.Authorization` (núcleo)

- **Entities + configurations:** `Permission`, `GroupPermission`, `Profile`,
  `PermissionProfile`, `UserProfile`.
- **`LumenAuthorizationDbContext`** (novo) com apenas esses `DbSet` + o filtro global de
  soft-delete (`ISoftDeletable`).
- **Repositórios:** `IPermissionRepository`, `IGroupPermissionRepository`,
  `IProfileRepository`, `IUserProfileRepository` + implementações.
- **Serviços de domínio/aplicação:** `IUserPermissionService`/`UserPermissionService`,
  `IUserPermissionCache`/`UserPermissionCache`, `IPermissionSyncService`/`PermissionSyncService`.
- **CQRS de autorização:** handlers de Profiles (`Create`/`Update`/`Delete`/`SetPermissions`),
  UserProfiles (`Assign`/`Remove`) e queries de leitura (`GetProfile`, `ListProfiles`,
  `ListPermissions`, `ListUserProfiles`, `GetAuthorizationGraph`).
- **Cache handler:** `UserPermissionsChangedCacheHandler` (invalidação em mudança de permissão).

### Migra para `Lumen.Authorization.AspNetCore`

- `RequirePermissionAttribute` e `PermissionGroupAttribute` (hoje em `SharedKernel`).
- `PermissionPolicyProvider`, `PermissionRequirement`, `PermissionAuthorizationHandler`,
  `PermissionEnforcementServiceCollectionExtensions`.
- `PermissionDiscoveryScanner` + `PermissionDiscoveryHostedService`.

### Permanece no módulo Identity / host

- **Autenticação e adjacências:** `User`, tokens (refresh/reset/confirmação), `IJwtService`,
  `IPasswordHasher`, `IPasswordValidator`, HIBP, e-mail/notificações, fluxos de Auth
  (login/register/refresh/logout/forgot/reset), `GetCurrentUser`, `ListUsers`, `GetUserDetail`.
- **`AuthorizationGraphHub` (SignalR)** e o push do grafo ao vivo continuam **no host** — são
  uma feature de apresentação do Lumen, não parte do contrato da lib. O handler de push
  permanece um `IIntegrationEventHandler<UserPermissionsChangedEvent>` do host.

---

## Contrato com o consumidor

- **A lib não faz autenticação.** O consumidor autentica como quiser (Cookies, JWT próprio,
  IdentityServer, etc.) e expõe o `userId` num claim. A lib lê esse claim (claim type
  configurável — ver LIB-08) para resolver as permissões.
- **`UserProfile.UserId` permanece `Guid`**, referência **opaca** ao usuário do consumidor,
  **sem FK** (a lib não possui tabela de usuários). O consumidor deve emitir o id como `Guid`
  no claim configurado. Suporte a id `string` fica como evolução futura, não neste escopo.
- **Redis é opcional** (ver LIB-04): sem connection string de Redis, o cache de permissão cai
  para `IDistributedCache` in-memory. "Basta a connection string do banco" para funcionar.
- **Auto-migração é opt-out:** ligada por padrão, com flag para desligar (consumidores que
  aplicam migrations no pipeline de deploy).

---

## Naming: schema e tabelas

Confirma o naming pedido: schema **`Lumen`**, tabelas no **singular** (`Permission`,
`PermissionGroup`, `Profile`, `PermissionProfile`, `UserProfile`) — divergindo do atual
schema `identity.*` com `Permissions`/`GroupPermissions` no plural. Para o consumidor é
greenfield (não há migração de dados). Para o próprio Lumen (dogfooding, LIB-12) a transição
`identity.*` → `Lumen.*` é tratada como recriação em dev, coerente com a política de "schema
legado órfão" já adotada na ADR-0003 para o `dbo.*`.

---

## Enforcement: um atributo em vez de dois

Hoje um endpoint protegido carrega **dois** atributos: `[RequirePermission]` (apenas
marcador para descoberta/seed) **e** `[Authorize(Policy = <code>)]` (o que efetivamente
barra, resolvido pelo `PermissionPolicyProvider`). A decisão é fundir em **um só**:
`[RequirePermission("...")]` passa a **declarar e enforçar** (ver LIB-06). A variante
`[Authorize(Policy = "Lumen:<code>")]` continua disponível para quem preferir policy nomeada.

---

## Alternativas consideradas

### A1 — Publicar o módulo Identity inteiro como lib

Descartada. Arrasta autenticação (JWT/e-mail/BCrypt/HIBP/Redis) e impõe o modelo de usuário
do Lumen ao consumidor, contrariando o requisito de "o consumidor traz o próprio login".
Autorização é a capacidade genuinamente reutilizável; autenticação é opinativa.

### A2 — Manter o enforcement no host e publicar só o núcleo

Descartada. O valor central para o consumidor é justamente `[RequirePermission]` + policy
provider + discovery. Sem o pacote `.AspNetCore`, o consumidor teria que reimplementar a cola
web — a lib deixaria de cumprir a promessa "basta o atributo".

### A3 — Backoffice como host separado (em vez de RCL montável)

Descartada como entrega principal (decisão do usuário). Um host separado não é "uma rota
dentro do app do consumidor". A RCL com Area + static web assets entrega a tela embutida via
`MapLumenBackoffice`. (Uma API REST-only sem UI fica como opção documentada, não default.)

### A4 — Núcleo dependente de ASP.NET (pacote único)

Descartada. Fundir núcleo e cola web num pacote só acopla o domínio de autorização ao
pipeline HTTP, prejudicando teste isolado e reuso fora de web. A separação núcleo vs
`.AspNetCore` custa um assembly a mais e paga em limpeza de fronteira.

---

## Consequências

### Positivas

- **Reuso real:** qualquer app ASP.NET Core ganha permissões + backoffice só com a connection
  string, sem herdar o sistema de auth do Lumen.
- **Fronteira limpa:** núcleo agnóstico de ASP.NET; cola web isolada; DbContext/schema próprios.
- **Ergonomia:** um atributo declara e enforça; migração e seed automáticos no startup.
- **Dogfooding:** o próprio Lumen passa a consumir a lib (LIB-12), validando a API pública.

### Negativas / trade-offs

- **Mais assemblies** (`Lumen.Authorization` × 4) e refatoração ampla do módulo Identity, que
  perde o domínio de autorização e passa a depender da lib.
- **Eventos compartilhados:** `UserProfileAssigned/Removed`, `ProfilePermissionsSet`,
  `UserPermissionsChanged` são publicados por handlers de autorização e **consumidos pelo
  Audit**. A extração precisa preservar esse contrato de eventos (o Audit não pode passar a
  depender de internals da lib) — resolver via Contratos públicos da lib + event bus.
- **`AuthorizationGraph`/SignalR** fica meio-a-meio: a query e o modelo migram para a lib, mas
  o hub e o push ao vivo ficam no host — a fronteira exata precisa de atenção no LIB-02/LIB-07.
- **Constantes de seed** (`SystemProfiles`, `Permissions`/`PermissionCodes`,
  `PermissionGroups`, `DatabaseSchemas`) hoje em `SharedKernel`: as pertinentes à autorização
  passam a acompanhar a lib (evitar que a lib dependa do `SharedKernel` do Lumen).
- **Transição de schema** `identity.*` → `Lumen.*` no ambiente do próprio Lumen é recriação em
  dev, não migração de dados.

### Projetos impactados

| Projeto | Impacto |
|---|---|
| `Lumen.Authorization` (novo) | Núcleo de autorização (entities, DbContext, repos, CQRS, cache, service) |
| `Lumen.Authorization.Migrations` (novo) | Migrations do schema `Lumen` + hosted service de auto-migração |
| `Lumen.Authorization.AspNetCore` (novo) | `[RequirePermission]`, policy provider/handler, discovery, `AddLumenAuthorization` |
| `Lumen.Authorization.Backoffice` (novo) | RCL montável (`MapLumenBackoffice`) |
| `Lumen.Modules.Identity[.Contracts]` | Perde o domínio de autorização; passa a depender da lib; mantém autenticação |
| `Lumen.SharedKernel` | `RequirePermissionAttribute`/`PermissionGroupAttribute` e constantes de authz migram para a lib |
| `Lumen.Api` | Remove `Authorization/*` local; consome a lib via `AddLumenAuthorization` |
| `Lumen.Backoffice` | Passa a montar a RCL em vez das telas próprias de permissão |
| `Lumen.ArchitectureTests` | Novas regras de fronteira para `Lumen.Authorization*` |

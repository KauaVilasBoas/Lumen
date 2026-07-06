# ADR-0006: Autorização tenant-scoped (profiles/permissões por scope)

**Status:** Accepted — 2026-07-06
**Related:** ADR-0004 (autorização como biblioteca), ADR-0005 (multi-provider), card #147 "[Authz] Profiles/permissions tenant-scoped (multi-company)"

---

## Contexto

A autorização atual é **global por usuário**: `UserProfile { UserId, ProfileId }` associa um
usuário a um perfil sem qualquer dimensão de tenant/company. O enforcement via
`[RequirePermission]` chama `IUserPermissionService.HasPermissionAsync(userId, code)`, que
consulta todas as permissões do usuário independentemente de contexto.

Esse modelo funciona para sistemas single-tenant. Para sistemas multi-company — onde um mesmo
usuário pode ter perfis distintos em cada empresa — o modelo global é insuficiente: um usuário
que é `Administrador` na empresa A mas `Visitante` na empresa B não deve ter permissões de admin
ao operar no contexto da empresa B.

### Requisitos principais

1. Um usuário pode ter diferentes perfis (e logo diferentes permissões) em diferentes
   scopes/tenants.
2. O enforcement por request deve considerar o **scope ativo** naquele contexto (determinado
   pelo host da lib, não pela lib em si).
3. **Retrocompatibilidade obrigatória:** apps sem tenant devem continuar funcionando exatamente
   como hoje — sem nenhuma mudança de configuração ou código.
4. A lib Lumen é genérica. A dimensão de "scope" é um `Guid?` opaco; a lib não conhece a
   semântica de negócio do que é um tenant.

---

## Decisão

### 1. `scopeId` nullable em `UserProfile`

Adicionar `Guid? ScopeId` ao aggregate `UserProfile`:

- `ScopeId = null` → assignment **global** (sem tenant). Comportamento idêntico ao atual.
- `ScopeId = <guid>` → assignment **scoped** (válido apenas para aquele tenant/company).

Um mesmo usuário pode ter múltiplos assignments em scopes distintos. A unicidade ativa é
garantida por índice filtrado em `(UserId, ProfileId, ScopeId)` com `IsDeleted = false`.

Os assignments globais existentes (`ScopeId = null`) permanecem válidos sem migração de dados.

### 2. `ITenantScopeAccessor` — ponto de extensão no host

Introduzir a interface pública `ITenantScopeAccessor` em `Lumen.Authorization.Contracts`:

```csharp
public interface ITenantScopeAccessor
{
    Guid? GetCurrentScopeId();
}
```

O default é um **no-op** que retorna `null` (comportamento global). Hosts com tenant
registram sua própria implementação via `services.AddScoped<ITenantScopeAccessor, MeuAccessor>()`.
Análogo ao `IUserIdAccessor` já existente.

A lib registra `NoOpTenantScopeAccessor` via `TryAddScoped`, garantindo que o host pode
sobrescrever sem conflito.

### 3. Resolução de permissões por `(userId, scopeId)`

`IUserPermissionService.GetPermissionsAsync` recebe `scopeId` opcional:

```csharp
Task<HashSet<string>> GetPermissionsAsync(
    Guid userId,
    Guid? scopeId = null,
    CancellationToken cancellationToken = default);
```

Quando `scopeId != null`, a query filtra `UserProfile` onde `ScopeId = scopeId`.
Quando `scopeId = null`, filtra `UserProfile` onde `ScopeId IS NULL` (global).

O handler de autorização (`PermissionAuthorizationHandler`) resolve o scope via
`ITenantScopeAccessor.GetCurrentScopeId()` e o passa para o serviço.

### 4. Cache chaveado por `(userId, scopeId)`

A chave de cache passa a ser `user-permissions:{userId}:{scopeId ?? "global"}`:

- `user-permissions:abc-123:global` → permissões globais do usuário
- `user-permissions:abc-123:scope-456` → permissões na empresa 456

Invalidação: ao mudar o assignment de um usuário, invalida **apenas a entrada afetada**
`(userId, scopeId)`, não todas as entradas do usuário. O evento `UserPermissionsChangedEvent`
ganha `Guid? ScopeId`.

### 5. Retrocompatibilidade total

- `UserProfile` sem `ScopeId` (legado) ≡ `ScopeId = null`. Nenhum dado existente precisa
  ser migrado.
- Apps sem tenant: não implementam `ITenantScopeAccessor`; o no-op retorna `null`; o handler
  passa `scopeId = null`; a query filtra global; o cache usa chave `...:global`. Fluxo
  idêntico ao atual.
- As assinaturas públicas existentes de `IUserPermissionService` recebem `scopeId` como
  **parâmetro opcional com default `null`**, portanto código existente que chama
  `HasPermissionAsync(userId, code)` **não quebra**.

---

## Alternativas consideradas

### A. Scope embutido no token JWT (claim)

O scope ativo viria como claim no JWT. Rejeitada: a lib não controla a emissão de tokens;
tornar o scope uma claim exige que o AuthN conheça o contexto de multi-tenant, acoplando
os dois domínios. O ponto de extensão (`ITenantScopeAccessor`) é mais desacoplado e permite
qualquer fonte (header HTTP, cookie, claim, context item, etc.).

### B. Múltiplas entradas de cache invalidadas em bloco por usuário

Manter invalidação por userId (invalida todas as entradas `user-permissions:{userId}:*`).
Aceito como fallback de implementação, mas requer scan de chaves Redis (padrão `SCAN`), que
não é suportado de forma padronizada pelo `IDistributedCache`. A invalidação por entrada
específica `(userId, scopeId)` é mais cirúrgica e não depende de capacidade de scan.

### C. Tabela separada `UserScopeProfile` para assignments scoped

Criar uma segunda tabela distinta da `UserProfile` global. Rejeitada: duplicação de lógica
de negócio, queries mais complexas, migrações adicionais. A coluna nullable `ScopeId` em
`UserProfile` é mais simples e expressiva.

### D. `scopeId` no request path / header, interpretado pela lib

A lib leria o scope de um header HTTP diretamente. Rejeitada: vincula a lib a ASP.NET Core
HTTP pipeline e a convenções de headers específicas. O accessor é agnóstico a transporte.

---

## Consequências

### Positivas

- Suporte multi-tenant aditivo: zero regressão para consumidores sem tenant.
- Ponto de extensão simples (`ITenantScopeAccessor`) compatível com qualquer fonte de scope.
- Cache eficiente por `(userId, scopeId)` — sem invalidação desnecessária de outros scopes.
- API pública estável: `scopeId` é parâmetro opcional; código existente não precisa mudar.

### Negativas / trade-offs

- **Dois assemblies de migration** ganham mais uma migration (SQL Server + PostgreSQL).
  Mitigação: frequência baixa após estabilização.
- **Invalidação não cobre ausência de scan:** ao mudar o perfil global de um usuário que
  também tem entries scoped em cache, as entries scoped não são invalidadas automaticamente.
  Mitigação: o evento `UserPermissionsChangedEvent` carrega `ScopeId`; o handler invalida
  apenas a entrada afetada. Se o host precisar de invalidação total, pode publicar múltiplos
  eventos ou implementar scan customizado.
- **Index de unicidade muda:** o índice filtrado em `(UserId, ProfileId)` passa para
  `(UserId, ProfileId, ScopeId)`. Migrations adicionais necessárias nos dois providers.

### Projetos impactados

| Projeto | Impacto |
|---|---|
| `Lumen.Authorization.Contracts` | `ITenantScopeAccessor` (novo); `IUserPermissionService` atualizado; `UserPermissionsChangedEvent` com `ScopeId` |
| `Lumen.Authorization` | `UserProfile.ScopeId`; query scoped no `ProfileRepository`; cache/service ciente de scope; no-op accessor registrado via `TryAddScoped` |
| `Lumen.Authorization.AspNetCore` | `PermissionAuthorizationHandler` resolve scope via `ITenantScopeAccessor` |
| `Lumen.Authorization.Migrations` | Nova migration SQL Server: `AddColumn ScopeId + novo índice` |
| `Lumen.Authorization.Migrations.PostgreSQL` | Nova migration PostgreSQL equivalente (snake_case) |
| `Lumen.Authorization.Tests` | Novos testes: scope ativo, cache por `(userId, scopeId)`, caminho global inalterado |

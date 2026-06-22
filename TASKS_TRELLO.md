# Lumen — Backlog Trello (.NET 8 Edition)

**Projeto:** Sistema de Autenticação de Usuários (Portfólio BoostProgram — Projeto 04, Nível Básico)
**Board sugerido:** https://trello.com/b/2ZZ0yCf8/portifolio-projects
**Owner:** Kauã (kauavboas@gmail.com)
**Data de geração:** 2026-05-17
**Revisão:** v2 — stack adaptada de Node.js para .NET 8

---

## Decisões de Stack confirmadas

| Camada | Tecnologia | Licença / Custo |
|---|---|---|
| Runtime | .NET 8.0 (LTS) | MIT — open source |
| Web framework | ASP.NET Core 8 — **Minimal APIs** (back-end) + **Razor Pages** (front-end) | MIT |
| Banco | MongoDB via **MongoDB.Driver** oficial | Apache 2.0 (driver) / SSPL (servidor) — Atlas M0 free para hospedagem ou Docker self-host |
| Auth | JWT via `Microsoft.AspNetCore.Authentication.JwtBearer` + `System.IdentityModel.Tokens.Jwt` | MIT |
| Hash de senha | **BCrypt.Net-Next** | MIT |
| Validação | **FluentValidation** | Apache 2.0 |
| Email (lib) | **MailKit** (MimeKit + MailKit) | MIT |
| Email (dev) | **Mailpit** via Docker | MIT |
| Email (prod) | **Postal** self-host em Docker (opção 100% open source) ou SMTP genérico via env vars (provedor escolhido pelo deploy) | MIT |
| Senhas vazadas | **HaveIBeenPwned Pwned Passwords API** (k-anonymity, gratuita) | Public API gratuita |
| Documentação API | **Swashbuckle.AspNetCore** (Swagger/OpenAPI) | MIT |
| Logging | **Serilog** (`Serilog.AspNetCore` + sinks) | Apache 2.0 |
| Testes | **xUnit** + **FluentAssertions** + **Testcontainers.MongoDb** | Apache 2.0 / MIT |
| Deploy | Docker + **Fly.io free tier** (recomendado) ou Render / Railway — self-host como fallback | open source na origem (Docker) |

**Por que Minimal APIs em vez de Controllers?** Menos boilerplate, ideal para escopo pequeno de portfólio, alinha com o "espírito Express" do PDF original. Se o projeto crescer e precisar de filtros/conventions mais complexos, migrar para Controllers depois é direto (mesmo modelo de injeção e roteamento).

**Por que Razor Pages e não MVC clássico?** Razor Pages é o substituto direto de EJS no mundo .NET — uma página = um arquivo `.cshtml` + um `.cshtml.cs` (page model). Server-rendered, simples, sem ginástica de controllers para telas pequenas (login, registro, dashboard).

**Por que MailKit e não `System.Net.Mail.SmtpClient`?** A Microsoft oficialmente marca `SmtpClient` como obsoleto/não recomendado para código novo. MailKit é o padrão de mercado, mantido pelo Jeffrey Stedfast, suporta TLS moderno, OAuth2, IDN, etc.

---

## Visão Geral — Listas sugeridas no Trello

```
[Backlog]  →  [To Do (Sprint Atual)]  →  [In Progress]  →  [Code Review]  →  [Done]
```

**Labels sugeridas (cores):**
- `backend` (azul)
- `frontend` (verde)
- `database` (laranja)
- `security` (vermelho)
- `infra` (cinza)
- `email` (azul claro)
- `docs` (amarelo)
- `tests` (roxo)
- `tech-debt` (preto)
- `bug` (rosa)

---

## Épicos e Caminho Crítico

| Épico | Total Tasks | Prioridade | Bloqueia |
|---|---|---|---|
| EP-01 Setup & Infra | 5 | Crítica | Todos os outros |
| EP-02 Camada de Dados (MongoDB.Driver) | 3 | Crítica | EP-03, EP-04 |
| EP-03 Auth Core (registro/login/JWT) | 7 | Crítica | EP-04, EP-05, EP-06 |
| EP-04 User Management (CRUD + roles) | 5 | Alta | EP-06 (parcial) |
| EP-05 Segurança Transversal | 6 | Alta | Deploy |
| EP-06 Frontend Razor Pages | 4 | Média | — |
| EP-07 Email, Recuperação, Reset & Ativação | 5 | Média | EP-03 concluído |
| EP-08 Testes (xUnit + Testcontainers) | 4 | Alta | Deploy |
| EP-09 Documentação | 3 | Média | — |
| EP-10 Deploy (Docker + Fly.io) | 3 | Alta | EP-01..05 concluídos |

**Total:** 10 épicos / **45 cards** (versão Node tinha 42; +1 Mailpit em EP-01, +1 HIBP em EP-05, +1 SMTP prod env vars em EP-07).

**Caminho crítico do MVP:**
`EP-01 → EP-02 → EP-03 → EP-05 (rate limit + headers + HIBP) → EP-08 (smoke) → EP-10`

EP-06 (Razor Pages), EP-07 (recuperação de senha) e EP-09 (docs) podem ser paralelizados após EP-03.

---

# EP-01 — Setup & Infra

---

## [SETUP-01] Limpar e estruturar solução .NET 8 do Lumen

- **Lista:** To Do
- **Labels:** `infra`, `backend`
- **Prioridade:** Crítica
- **Estimativa:** P (1-2h)
- **Branch sugerida:** `chore/setup-solution-structure`
- **Commit sugerido:** `chore: structure Lumen solution into layered projects`

**Descrição:**
A solução `Lumen.sln` hoje tem um único `Lumen.csproj` (`Microsoft.NET.Sdk.Web`, net8.0) com um `Program.cs` praticamente vazio (`MapGet("/", () => "Hello World!")`). Reorganizar em camadas mantendo Minimal APIs como entrypoint, separando contratos, domínio, infraestrutura e testes.

**Critérios de Aceite:**
- [ ] Estrutura de projetos criada na solução:
  - `src/Lumen.Api` (Web, Minimal APIs, Razor Pages — projeto principal atual renomeado)
  - `src/Lumen.Domain` (classes de domínio puras, sem dependências externas)
  - `src/Lumen.Infrastructure` (MongoDB, Email, integrações externas)
  - `tests/Lumen.UnitTests`
  - `tests/Lumen.IntegrationTests`
- [ ] `.gitignore` cobre `bin/`, `obj/`, `*.user`, `.idea/`, `.vs/`, `.vscode/`, `appsettings.*.local.json`, `.env`, `secrets.json`
- [ ] `Directory.Build.props` na raiz com `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<LangVersion>latest</LangVersion>`
- [ ] `Directory.Packages.props` com Central Package Management ativado
- [ ] README.md atualizado com seções "Sobre", "Stack (.NET 8)", "Como rodar"
- [ ] Arquivo `LICENSE` (MIT)
- [ ] `dotnet build` passa sem warnings

**Notas Técnicas:**
- Arquitetura: Clean Architecture leve. Api → Domain (sem dependências). Api + Infrastructure → Domain. Tests → todos.
- Namespaces seguem o nome do projeto: `Lumen.Api.Endpoints.Auth`, `Lumen.Domain.Users`, etc.
- O `Program.cs` atual fica em `Lumen.Api/Program.cs`.

**Dependências:** —
**Bloqueia:** Todas as demais

**Riscos:**
- Quebrar build atual ao mover arquivos. Mitigação: commit pequeno, testar `dotnet build` após cada movimento.

---

## [SETUP-02] Configurar Central Package Management e dependências base

- **Lista:** To Do
- **Labels:** `infra`, `backend`
- **Prioridade:** Crítica
- **Estimativa:** P (1h)
- **Branch sugerida:** `chore/setup-packages`
- **Commit sugerido:** `chore: add base NuGet packages and central package management`

**Descrição:**
Adicionar todas as dependências NuGet do MVP via `Directory.Packages.props`, fixando versões e centralizando para evitar drift entre projetos.

**Critérios de Aceite:**
- [ ] `Directory.Packages.props` versiona ao menos:
  - `MongoDB.Driver` (2.x mais recente)
  - `Microsoft.AspNetCore.Authentication.JwtBearer` (8.x)
  - `System.IdentityModel.Tokens.Jwt` (mais recente compatível com .NET 8)
  - `BCrypt.Net-Next`
  - `FluentValidation` + `FluentValidation.AspNetCore`
  - `MailKit` (inclui MimeKit)
  - `Swashbuckle.AspNetCore`
  - `Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`
  - `AspNetCoreRateLimit` (rate limit; ou alternativa `Microsoft.AspNetCore.RateLimiting` nativa do .NET 8)
  - `Microsoft.Extensions.Http` (HttpClient factory para HIBP)
- [ ] Pacotes de teste (no `tests/.../*.csproj`):
  - `xunit`, `xunit.runner.visualstudio`
  - `FluentAssertions`
  - `Microsoft.AspNetCore.Mvc.Testing`
  - `Testcontainers.MongoDb`
  - `NSubstitute` (mocking)
- [ ] `dotnet restore` e `dotnet build` passam limpos
- [ ] Nenhum aviso de vulnerabilidade de pacote (`dotnet list package --vulnerable`)

**Notas Técnicas:**
- Recomendação: usar `Microsoft.AspNetCore.RateLimiting` nativa (lançada no .NET 7) em vez de `AspNetCoreRateLimit`, é menos pacote externo. Decidir no card de rate limit (SEC-01).

**Dependências:** SETUP-01
**Bloqueia:** Todos os cards que dependem de bibliotecas

**Riscos:** —

---

## [SETUP-03] Configurar variáveis de ambiente e `appsettings`

- **Lista:** To Do
- **Labels:** `infra`, `security`
- **Prioridade:** Crítica
- **Estimativa:** P (1h)
- **Branch sugerida:** `chore/setup-config`
- **Commit sugerido:** `chore: setup configuration and environment variables`

**Descrição:**
Definir o esquema de configuração em `appsettings.json` + `appsettings.Development.json` + variáveis de ambiente para produção. Nenhum segredo no repositório.

**Critérios de Aceite:**
- [ ] `appsettings.json` define seções (com valores placeholder seguros):
  - `Mongo:ConnectionString`, `Mongo:Database`
  - `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Secret` (placeholder), `Jwt:ExpirationMinutes`, `Jwt:RefreshExpirationDays`
  - `Smtp:Host`, `Smtp:Port`, `Smtp:User`, `Smtp:Pass`, `Smtp:From`, `Smtp:UseStartTls`
  - `Hibp:UserAgent`, `Hibp:ApiBaseUrl`
  - `Cors:AllowedOrigins`
  - `Serilog` (configuração de sinks)
- [ ] `appsettings.Development.json` aponta Mongo para `mongodb://localhost:27017` e SMTP para Mailpit (`localhost:1025`)
- [ ] `.env.example` ou `appsettings.example.json` versionado mostrando o formato
- [ ] **Segredos via User Secrets em dev** (`dotnet user-secrets`), **via variáveis de ambiente em prod** (formato `Jwt__Secret`, `Mongo__ConnectionString` etc.)
- [ ] Validação on-startup: classe `JwtOptions`, `MongoOptions`, `SmtpOptions` com `[Required]` validadas via `ValidateOnStart()`
- [ ] README documenta variáveis obrigatórias

**Notas Técnicas:**
- Padrão: `services.AddOptions<JwtOptions>().Bind(config.GetSection("Jwt")).ValidateDataAnnotations().ValidateOnStart();`
- Nunca commitar valores reais. Secrets em produção via env vars do provedor (Fly.io secrets, etc.).

**Dependências:** SETUP-01
**Bloqueia:** Todos os endpoints que leem config

**Riscos:**
- Vazar segredo no git. Mitigação: `.gitignore` + revisão obrigatória de qualquer arquivo com nome "settings" ou "env".

---

## [SETUP-04] Configurar Serilog para logs estruturados

- **Lista:** To Do
- **Labels:** `infra`, `backend`, `tech-debt`
- **Prioridade:** Alta
- **Estimativa:** P (1h)
- **Branch sugerida:** `chore/logging-serilog`
- **Commit sugerido:** `chore: configure Serilog with structured logging`

**Descrição:**
Substituir o logger padrão pelo Serilog para ter logs estruturados (JSON em prod, console legível em dev), com correlação de request id e captura de exceções.

**Critérios de Aceite:**
- [ ] `Program.cs` configura Serilog via `UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration))`
- [ ] Em dev: sink `Console` com formato legível
- [ ] Em prod: sink `Console` em **CompactJsonFormatter** + sink `File` rotativo diário (`logs/aegis-.log`, 7 dias retidos)
- [ ] Middleware `UseSerilogRequestLogging` ativo
- [ ] Logs não podem conter senha em claro, hash de senha, token JWT, refresh token, código de reset
- [ ] Logger enriquece com `MachineName`, `ThreadId`, `CorrelationId` (header `X-Correlation-Id` ou gerado)

**Notas Técnicas:**
- Adicionar `Serilog.Enrichers.Environment`, `Serilog.Enrichers.Thread`.
- Filtros: ignorar health checks no log de request para não poluir.

**Dependências:** SETUP-02, SETUP-03
**Bloqueia:** Observabilidade nos demais cards

**Riscos:** —

---

## [SETUP-05] Setup Mailpit local via Docker Compose

- **Lista:** To Do
- **Labels:** `infra`, `email`, `backend`
- **Prioridade:** Alta
- **Estimativa:** P (1h)
- **Branch sugerida:** `chore/mailpit-dev`
- **Commit sugerido:** `chore: add Mailpit dev SMTP server via docker compose`

**Descrição:**
Subir o **Mailpit** (substituto moderno do MailHog, MIT) como servidor SMTP de desenvolvimento via `docker-compose.yml`. O Mailpit aceita qualquer email enviado pela app e mostra na UI web local — zero credenciais, zero risco de enviar para usuários reais por engano.

**Critérios de Aceite:**
- [ ] Arquivo `docker-compose.yml` na raiz do projeto define serviço `mailpit`:
  - Imagem `axllent/mailpit:latest`
  - Porta SMTP `1025` exposta em `localhost:1025`
  - Porta UI HTTP `8025` exposta em `localhost:8025`
  - Volume opcional para persistir mensagens (`./.mailpit-data:/data`)
  - `restart: unless-stopped`
- [ ] Mesmo compose define serviço `mongo` (MongoDB local) na porta `27017` com volume persistente — facilita rodar tudo num comando.
- [ ] `appsettings.Development.json` aponta `Smtp:Host=localhost`, `Smtp:Port=1025`, `Smtp:UseStartTls=false` (Mailpit não exige TLS)
- [ ] README.md documenta:
  - Como subir: `docker compose up -d`
  - Como derrubar: `docker compose down`
  - URL da UI: http://localhost:8025
- [ ] Smoke test manual: enviar um email pelo endpoint de teste (criar um `/dev/email-test` em `Development` only) e abrir o Mailpit para validar recebimento.

**Notas Técnicas:**
- Por que Mailpit e não MailHog: MailHog está sem manutenção desde 2020. Mailpit é o sucessor (mesma proposta, código moderno, UI melhor, busca full-text).
- Endpoint `/dev/email-test` deve ser registrado apenas em `app.Environment.IsDevelopment()`.
- O `volume` do Mailpit é opcional; se omitido, mensagens são limpas a cada restart do container (geralmente desejável em dev).

**Dependências:** SETUP-02
**Bloqueia:** Todos os cards que enviam email (EP-07)

**Riscos:**
- Esquecer de configurar SMTP em produção e o app tentar mandar para `localhost:1025`. Mitigação: validação on-startup em `SmtpOptions` exige `Host` não vazio e diferente de `localhost` quando `ASPNETCORE_ENVIRONMENT=Production`.

---

# EP-02 — Camada de Dados (MongoDB.Driver)

---

## [DATA-01] Configurar conexão MongoDB e DI

- **Lista:** To Do
- **Labels:** `database`, `backend`
- **Prioridade:** Crítica
- **Estimativa:** P (1-2h)
- **Branch sugerida:** `feat/mongo-connection`
- **Commit sugerido:** `feat(infra): configure MongoDB client and DI`

**Descrição:**
Registrar `IMongoClient` como singleton, expor `IMongoDatabase` via DI, garantir reconexão automática e health check.

**Critérios de Aceite:**
- [ ] `services.AddSingleton<IMongoClient>(sp => new MongoClient(options.ConnectionString))`
- [ ] `services.AddScoped(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(options.Database))`
- [ ] Convenções de serialização registradas no startup:
  - `CamelCaseElementNameConvention`
  - `IgnoreExtraElementsConvention`
  - `EnumRepresentationConvention(BsonType.String)`
- [ ] Health check `/health/db` que faz `db.RunCommand({ ping: 1 })`
- [ ] Logs de erro de conexão tratados sem expor connection string
- [ ] Connection string lida apenas de config/env vars, nunca hardcoded

**Notas Técnicas:**
- Convenções registradas via `ConventionRegistry.Register("camelCase", pack, t => true);` antes de qualquer mapping.
- Não usar `IMongoClient` como Scoped — driver oficial recomenda singleton (pool interno).

**Dependências:** SETUP-02, SETUP-03
**Bloqueia:** DATA-02, DATA-03

**Riscos:** —

---

## [DATA-02] Modelar entidade `User` e índices

- **Lista:** To Do
- **Labels:** `database`, `backend`
- **Prioridade:** Crítica
- **Estimativa:** M (3-4h)
- **Branch sugerida:** `feat/user-entity`
- **Commit sugerido:** `feat(domain): add User entity and persistence mapping`

**Descrição:**
Modelar a classe `User` em `Lumen.Domain.Users`, definir índices da collection `users` e mapeamentos BSON.

**Critérios de Aceite:**
- [ ] Classe `User` com propriedades:
  - `Id` (ObjectId)
  - `Email` (string, sempre lowercased)
  - `Username` (string)
  - `PasswordHash` (string)
  - `Roles` (List<string>, default `["user"]`)
  - `IsActive` (bool, default false até confirmar email)
  - `EmailConfirmedAt` (DateTime?)
  - `LastLoginAt` (DateTime?)
  - `FailedLoginAttempts` (int)
  - `LockedUntil` (DateTime?)
  - `CreatedAt`, `UpdatedAt` (DateTime)
- [ ] Mapeamento BSON via `BsonClassMap.RegisterClassMap<User>` em `Lumen.Infrastructure.Persistence.Mappings`
- [ ] Índices criados on-startup via `IHostedService` (`MongoIndexInitializer`):
  - `email` UNIQUE
  - `username` UNIQUE
  - `lockedUntil` (para query de unlock automático, sparse)
- [ ] Repositório `IUserRepository` definido em Domain, implementado em Infrastructure:
  - `Task<User?> FindByEmailAsync(string email, CancellationToken ct)`
  - `Task<User?> FindByIdAsync(string id, CancellationToken ct)`
  - `Task<User?> FindByUsernameAsync(string username, CancellationToken ct)`
  - `Task InsertAsync(User user, CancellationToken ct)`
  - `Task UpdateAsync(User user, CancellationToken ct)`

**Notas Técnicas:**
- Email **sempre** normalizado (lowercased + trim) antes de salvar/consultar.
- `Roles` simples para MVP. Se virar complexo, mover para enum + serialização customizada.
- Não usar `Identity` do ASP.NET Core — overkill para o escopo do PDF; manter modelo próprio simples.

**Dependências:** DATA-01
**Bloqueia:** AUTH-01, USER-*

**Riscos:**
- Index UNIQUE em `username` falhar se permitirmos username opcional. Decisão: username obrigatório no MVP.

---

## [DATA-03] Modelar entidades auxiliares (`RefreshToken`, `PasswordResetToken`, `EmailConfirmationToken`)

- **Lista:** To Do
- **Labels:** `database`, `backend`, `security`
- **Prioridade:** Alta
- **Estimativa:** M (2-3h)
- **Branch sugerida:** `feat/token-entities`
- **Commit sugerido:** `feat(domain): add refresh, reset and confirmation token entities`

**Descrição:**
Criar entidades para tokens auxiliares com TTL no MongoDB.

**Critérios de Aceite:**
- [ ] `RefreshToken`: `Id`, `UserId`, `TokenHash` (SHA-256 do token, nunca o token bruto), `ExpiresAt`, `CreatedAt`, `RevokedAt?`, `ReplacedByTokenHash?`, `CreatedByIp`
- [ ] `PasswordResetToken`: `Id`, `UserId`, `TokenHash`, `ExpiresAt`, `UsedAt?`
- [ ] `EmailConfirmationToken`: `Id`, `UserId`, `TokenHash`, `ExpiresAt`, `UsedAt?`
- [ ] Índice TTL no campo `ExpiresAt` para limpeza automática (`ExpireAfter = TimeSpan.Zero`)
- [ ] Repositórios correspondentes definidos em Domain, implementados em Infrastructure
- [ ] Nunca persistir o token em claro — apenas o hash SHA-256

**Notas Técnicas:**
- Hash SHA-256 é suficiente aqui porque o token é gerado randômico de alta entropia (32 bytes via `RandomNumberGenerator.GetBytes`), não precisa de slow hash como BCrypt.
- TTL no Mongo: `Builders<RefreshToken>.IndexKeys.Ascending(x => x.ExpiresAt)` com `CreateIndexOptions { ExpireAfter = TimeSpan.Zero }`.

**Dependências:** DATA-01, DATA-02
**Bloqueia:** AUTH-04 (refresh), AUTH-07 (reset)

**Riscos:**
- TTL do Mongo não é instantâneo (~60s). Sempre validar `ExpiresAt > now` no código também.

---

# EP-03 — Auth Core

---

## [AUTH-01] Endpoint de registro `POST /api/auth/register`

- **Lista:** To Do
- **Labels:** `backend`, `security`
- **Prioridade:** Crítica
- **Estimativa:** M (3-4h)
- **Branch sugerida:** `feat/auth-register`
- **Commit sugerido:** `feat(auth): add user registration endpoint`

**Descrição:**
Endpoint Minimal API para criar usuário com email + username + senha. Aplica política de senha forte (SEC-04), envia email de confirmação (EP-07).

**Critérios de Aceite:**
- [ ] Endpoint `POST /api/auth/register` aceita JSON `{ email, username, password }`
- [ ] Validação via FluentValidation (`RegisterRequestValidator`):
  - email válido (RFC 5322)
  - username 3-30 chars, alfanumérico + underscore
  - password atende política forte (delegado ao `IPasswordValidator`)
- [ ] Hash da senha via BCrypt.Net-Next com cost factor 12
- [ ] Retorna 201 Created com `{ id, email, username }` (nunca o hash, nunca o token de confirmação)
- [ ] Retorna 409 Conflict se email OU username já existirem (mensagem genérica para não fazer user enumeration: "Email ou username já em uso")
- [ ] Retorna 400 com lista de erros de validação
- [ ] Cria usuário com `IsActive=false`, gera `EmailConfirmationToken`, dispara envio de email (fire-and-forget com log de erro)
- [ ] Logs estruturados (sem senha, sem token)

**Notas Técnicas:**
- BCrypt cost 12 = ~250ms hash, balanço razoável para MVP. Subir para 13/14 se preocupar com brute force em prod.
- Endpoint **não** loga o usuário automaticamente após registro — exige confirmação de email primeiro.

**Dependências:** DATA-02, DATA-03, SEC-04 (password policy), EMAIL-01
**Bloqueia:** AUTH-02

**Riscos:**
- Race condition em UNIQUE index. Mitigação: tratar `MongoWriteException` com `Code == 11000` e retornar 409.

---

## [AUTH-02] Endpoint de login `POST /api/auth/login`

- **Lista:** To Do
- **Labels:** `backend`, `security`
- **Prioridade:** Crítica
- **Estimativa:** M (3-4h)
- **Branch sugerida:** `feat/auth-login`
- **Commit sugerido:** `feat(auth): add login endpoint with JWT issuance`

**Descrição:**
Endpoint de login que verifica credenciais, emite access token JWT + refresh token, aplica anti-brute-force com lockout.

**Critérios de Aceite:**
- [ ] `POST /api/auth/login` aceita `{ emailOrUsername, password }`
- [ ] Busca usuário por email **ou** username
- [ ] Verifica `BCrypt.Verify(password, user.PasswordHash)`
- [ ] Se `IsActive=false`, retorna 403 "Email não confirmado"
- [ ] Se `LockedUntil > now`, retorna 423 Locked com retry-after
- [ ] Senha errada: incrementa `FailedLoginAttempts`. Ao atingir 5 tentativas, define `LockedUntil = now + 15min` e zera contador
- [ ] Login OK: reseta `FailedLoginAttempts=0`, `LockedUntil=null`, atualiza `LastLoginAt`
- [ ] Retorna 200 com `{ accessToken, refreshToken, expiresIn, tokenType: "Bearer" }`
- [ ] Mensagem de erro genérica em 401: "Credenciais inválidas" (não diferenciar email errado vs senha errada)
- [ ] Refresh token gerado por `RandomNumberGenerator.GetBytes(32)` → base64url; armazena hash SHA-256
- [ ] Tempo constante: mesmo se usuário não existir, executar BCrypt.Verify contra um hash dummy para evitar timing attack

**Notas Técnicas:**
- JWT claims: `sub` (user id), `email`, `username`, `roles`, `jti` (id único), `iat`, `exp`, `iss`, `aud`.
- Access token: 15min. Refresh token: 7 dias.
- Helper `IJwtTokenService.GenerateAccessToken(User)`.

**Dependências:** AUTH-01, DATA-03
**Bloqueia:** AUTH-03, AUTH-04

**Riscos:**
- Lockout pode ser usado para DoS contra usuários legítimos (atacante propositalmente erra senha do alvo). Mitigação: rate limit por IP em SEC-01.

---

## [AUTH-03] Serviço de geração e validação de JWT

- **Lista:** To Do
- **Labels:** `backend`, `security`
- **Prioridade:** Crítica
- **Estimativa:** M (2-3h)
- **Branch sugerida:** `feat/jwt-service`
- **Commit sugerido:** `feat(auth): add JWT token service`

**Descrição:**
Encapsular geração e parsing de JWT num serviço único, registrado via DI.

**Critérios de Aceite:**
- [ ] `IJwtTokenService` em Domain com `GenerateAccessToken(User)`, `ValidateToken(string)`
- [ ] Implementação em Infrastructure usando `JwtSecurityTokenHandler`
- [ ] `JwtBearerOptions` configurado no `Program.cs`:
  - `ValidateIssuer = true`
  - `ValidateAudience = true`
  - `ValidateLifetime = true`
  - `ValidateIssuerSigningKey = true`
  - `ClockSkew = TimeSpan.FromSeconds(30)`
- [ ] Algoritmo: **HS256** (HMAC SHA256) com secret de pelo menos 256 bits (32 bytes) — validado on-startup
- [ ] `Program.cs` chama `app.UseAuthentication(); app.UseAuthorization();`
- [ ] Endpoint protegido de teste `/api/me` retorna claims do usuário autenticado (`[Authorize]`)

**Notas Técnicas:**
- HS256 é suficiente para MVP single-issuer. Migrar para RS256 se precisar federar (não é o caso aqui).
- Secret nunca em config — sempre em user-secrets (dev) ou env var (prod).

**Dependências:** SETUP-03
**Bloqueia:** AUTH-02, AUTH-04, qualquer endpoint protegido

**Riscos:**
- Secret fraco. Mitigação: validação on-startup com tamanho mínimo de 32 bytes.

---

## [AUTH-04] Endpoint de refresh token `POST /api/auth/refresh`

- **Lista:** To Do
- **Labels:** `backend`, `security`
- **Prioridade:** Alta
- **Estimativa:** M (3h)
- **Branch sugerida:** `feat/auth-refresh`
- **Commit sugerido:** `feat(auth): add refresh token endpoint with rotation`

**Descrição:**
Troca um refresh token válido por um novo par (access + refresh). Implementa rotação: refresh token antigo é revogado, novo é emitido.

**Critérios de Aceite:**
- [ ] `POST /api/auth/refresh` aceita `{ refreshToken }`
- [ ] Calcula SHA-256 do token recebido e busca no repositório
- [ ] Rejeita se: não existe, `ExpiresAt <= now`, `RevokedAt != null`
- [ ] Se token já revogado mas ainda dentro da validade → **revoga toda a família** do refresh token (replay detection) e retorna 401
- [ ] Sucesso: emite novo par, marca antigo `RevokedAt=now`, `ReplacedByTokenHash=<hash novo>`
- [ ] Logs incluem `userId`, IP origem, jamais o token

**Notas Técnicas:**
- "Família" de refresh token = cadeia via `ReplacedByTokenHash`. Detecta uso de token antigo (replay).
- Considerar token binding por IP em versões futuras (não no MVP).

**Dependências:** AUTH-02, AUTH-03, DATA-03
**Bloqueia:** —

**Riscos:**
- Cliente perde a corrida e tenta refresh com token já rotacionado. Mitigação: tolerância pequena de 5s (validar pelo `CreatedAt` recente) — opcional, decidir na implementação.

---

## [AUTH-05] Endpoint de logout `POST /api/auth/logout`

- **Lista:** To Do
- **Labels:** `backend`, `security`
- **Prioridade:** Alta
- **Estimativa:** P (1-2h)
- **Branch sugerida:** `feat/auth-logout`
- **Commit sugerido:** `feat(auth): add logout endpoint with refresh token revocation`

**Descrição:**
Revoga o refresh token enviado pelo cliente. Access token continua válido até expirar (15min) — para revogar imediato precisaríamos de blacklist, que fica fora do MVP.

**Critérios de Aceite:**
- [ ] `POST /api/auth/logout` (autenticado) aceita `{ refreshToken }`
- [ ] Valida ownership: refresh token deve pertencer ao `sub` do JWT
- [ ] Marca `RevokedAt=now`
- [ ] Retorna 204 No Content
- [ ] Logout sem refresh token (caso o cliente perdeu): aceita e retorna 204 mesmo assim (idempotente)

**Notas Técnicas:**
- Documentar no README que access token continua válido até `exp` — uso real exige blacklist via Redis (out of scope MVP).

**Dependências:** AUTH-04
**Bloqueia:** —

**Riscos:** —

---

## [AUTH-06] Endpoint `GET /api/me`

- **Lista:** To Do
- **Labels:** `backend`
- **Prioridade:** Média
- **Estimativa:** P (1h)
- **Branch sugerida:** `feat/auth-me`
- **Commit sugerido:** `feat(auth): add current user endpoint`

**Descrição:**
Retorna dados do usuário autenticado a partir do JWT.

**Critérios de Aceite:**
- [ ] `[Authorize]` em `GET /api/me`
- [ ] Lê `sub` do `ClaimsPrincipal`, busca user no repo, retorna DTO `{ id, email, username, roles, createdAt }`
- [ ] 401 se sem token / token inválido
- [ ] 404 se token tem `sub` de usuário deletado

**Notas Técnicas:**
- Padrão de DTO: `UserResponse` (sem campos sensíveis).

**Dependências:** AUTH-03
**Bloqueia:** —

**Riscos:** —

---

## [AUTH-07] Política de autorização baseada em roles

- **Lista:** To Do
- **Labels:** `backend`, `security`
- **Prioridade:** Alta
- **Estimativa:** P (1-2h)
- **Branch sugerida:** `feat/auth-roles`
- **Commit sugerido:** `feat(auth): add role-based authorization policies`

**Descrição:**
Configurar políticas de autorização para endpoints sensíveis (CRUD de outros usuários, listagem).

**Critérios de Aceite:**
- [ ] Policies registradas em `Program.cs`:
  - `RequireAdmin` (claim `role == "admin"`)
  - `RequireUser` (autenticado)
- [ ] `[Authorize(Policy = "RequireAdmin")]` nos endpoints administrativos
- [ ] Testes manuais (curl/Swagger) confirmam 403 para user comum em endpoint admin
- [ ] Seed opcional de usuário admin via comando CLI (`dotnet run -- seed-admin --email x --password y`) — só em Development

**Notas Técnicas:**
- Claims de role no JWT como array. Configurar `RoleClaimType = "role"` se necessário.

**Dependências:** AUTH-03
**Bloqueia:** USER-*

**Riscos:** —

---

# EP-04 — User Management

---

## [USER-01] `GET /api/users` (admin) — listagem paginada

- **Lista:** Backlog
- **Labels:** `backend`
- **Prioridade:** Média
- **Estimativa:** M (2-3h)
- **Branch sugerida:** `feat/users-list`
- **Commit sugerido:** `feat(users): add paginated user listing for admins`

**Descrição:**
Listagem paginada de usuários, apenas para admin.

**Critérios de Aceite:**
- [ ] `GET /api/users?page=1&pageSize=20&search=foo`
- [ ] `[Authorize(Policy = "RequireAdmin")]`
- [ ] Validação: `pageSize` máx 100
- [ ] Retorna `{ items, page, pageSize, totalCount, totalPages }`
- [ ] `search` filtra por email ou username (`$regex` case-insensitive, ancorado no começo para usar índice)
- [ ] Nunca expor `passwordHash`, `failedLoginAttempts`, `lockedUntil` no response

**Notas Técnicas:**
- DTO `UserListItemResponse`.
- Skip/limit OK para MVP. Cursor-based pagination fica para evolução.

**Dependências:** AUTH-07
**Bloqueia:** —

**Riscos:**
- `$regex` sem âncora `^` é full scan. Sempre ancorar.

---

## [USER-02] `GET /api/users/{id}` (admin ou owner)

- **Lista:** Backlog
- **Labels:** `backend`
- **Prioridade:** Média
- **Estimativa:** P (1-2h)
- **Branch sugerida:** `feat/users-get`
- **Commit sugerido:** `feat(users): add get user by id endpoint`

**Descrição:**
Busca usuário por id. Admin acessa qualquer um; user comum só o próprio.

**Critérios de Aceite:**
- [ ] `[Authorize]` + checagem de ownership no handler
- [ ] 403 se user comum tenta acessar id diferente do próprio `sub`
- [ ] 404 se não encontrado
- [ ] Response `UserResponse` (sem campos sensíveis)

**Dependências:** AUTH-07
**Bloqueia:** —

**Riscos:**
- IDOR. Mitigação: checagem explícita de ownership, teste de integração cobrindo o caso.

---

## [USER-03] `PUT /api/users/{id}` — atualização de dados

- **Lista:** Backlog
- **Labels:** `backend`
- **Prioridade:** Média
- **Estimativa:** M (2-3h)
- **Branch sugerida:** `feat/users-update`
- **Commit sugerido:** `feat(users): add user update endpoint`

**Descrição:**
Atualizar `username` e `email`. Mudança de email **exige reconfirmação** (manda novo `EmailConfirmationToken`, marca `IsActive=false` até confirmar).

**Critérios de Aceite:**
- [ ] Endpoint autenticado + ownership (ou admin)
- [ ] Validação FluentValidation
- [ ] Conflito 409 se novo email/username já em uso
- [ ] Mudança de email: gera novo token de confirmação, envia email, desativa conta
- [ ] Logs de auditoria: `userId`, campos alterados, quem alterou
- [ ] Senha **não** se altera por aqui (vai para USER-05)

**Dependências:** AUTH-07, EMAIL-01
**Bloqueia:** —

**Riscos:** —

---

## [USER-04] `DELETE /api/users/{id}` — soft delete

- **Lista:** Backlog
- **Labels:** `backend`, `security`
- **Prioridade:** Média
- **Estimativa:** P (1-2h)
- **Branch sugerida:** `feat/users-delete`
- **Commit sugerido:** `feat(users): add user soft delete`

**Descrição:**
Soft delete (marca `DeletedAt`) — não remove dados do banco para preservar histórico/integridade referencial e atender LGPD com janela de 30 dias antes de hard delete.

**Critérios de Aceite:**
- [ ] Admin ou owner pode deletar
- [ ] Campo `DeletedAt` adicionado ao `User`
- [ ] Filtro padrão em todas as queries: `DeletedAt == null`
- [ ] Refresh tokens do usuário são todos revogados na hora
- [ ] Retorna 204
- [ ] Endpoint admin `POST /api/users/{id}/restore` reverte soft delete dentro de 30 dias

**Dependências:** AUTH-07
**Bloqueia:** —

**Riscos:**
- Esquecer o filtro `DeletedAt == null` em alguma query. Mitigação: helper no repositório (`AsQueryable().NotDeleted()`).

---

## [USER-05] `POST /api/users/me/change-password`

- **Lista:** Backlog
- **Labels:** `backend`, `security`
- **Prioridade:** Alta
- **Estimativa:** M (2-3h)
- **Branch sugerida:** `feat/users-change-password`
- **Commit sugerido:** `feat(users): add change password endpoint`

**Descrição:**
Alterar senha do próprio usuário, exigindo senha atual + nova senha. Aplica política forte e revoga todos os refresh tokens.

**Critérios de Aceite:**
- [ ] `POST /api/users/me/change-password` autenticado
- [ ] Body: `{ currentPassword, newPassword }`
- [ ] Verifica `currentPassword` via BCrypt
- [ ] Aplica política de senha forte na nova senha (`IPasswordValidator`)
- [ ] Nova senha não pode ser igual à atual
- [ ] Atualiza `PasswordHash`, revoga TODOS os refresh tokens do usuário (logout em outros dispositivos)
- [ ] Envia email de notificação "Sua senha foi alterada" (não bloqueia o response)
- [ ] Retorna 204

**Dependências:** AUTH-03, SEC-04, EMAIL-01
**Bloqueia:** —

**Riscos:** —

---

# EP-05 — Segurança Transversal

---

## [SEC-01] Rate limiting global e por endpoint

- **Lista:** To Do
- **Labels:** `security`, `backend`
- **Prioridade:** Crítica
- **Estimativa:** M (2-3h)
- **Branch sugerida:** `feat/rate-limiting`
- **Commit sugerido:** `feat(security): add rate limiting policies`

**Descrição:**
Aplicar `Microsoft.AspNetCore.RateLimiting` nativo do .NET 8 com policies por endpoint sensível.

**Critérios de Aceite:**
- [ ] Policy `fixed-global`: 100 req/min por IP (resposta 429)
- [ ] Policy `login`: 5 req/min por IP em `POST /api/auth/login`
- [ ] Policy `register`: 3 req/min por IP em `POST /api/auth/register`
- [ ] Policy `forgot-password`: 3 req/min por IP em `POST /api/auth/forgot-password`
- [ ] Header `Retry-After` no 429
- [ ] Em prod, partition key inclui IP atrás de proxy: usar `X-Forwarded-For` via `ForwardedHeaders` middleware

**Notas Técnicas:**
- `app.UseRateLimiter()` antes de `UseAuthentication()`.
- Em Fly.io, configurar trust de `X-Forwarded-For` apenas do range interno.

**Dependências:** SETUP-02
**Bloqueia:** Deploy

**Riscos:**
- IP único atrás de NAT corporativo afeta muitos usuários legítimos. Documentar como limitação conhecida do MVP.

---

## [SEC-02] Headers de segurança (HSTS, CSP, X-Frame-Options, etc.)

- **Lista:** To Do
- **Labels:** `security`, `backend`
- **Prioridade:** Alta
- **Estimativa:** P (1-2h)
- **Branch sugerida:** `feat/security-headers`
- **Commit sugerido:** `feat(security): add security HTTP headers`

**Descrição:**
Middleware customizado para aplicar headers de segurança em todas as respostas.

**Critérios de Aceite:**
- [ ] `Strict-Transport-Security: max-age=63072000; includeSubDomains; preload` (apenas em prod via `app.UseHsts()`)
- [ ] `X-Content-Type-Options: nosniff`
- [ ] `X-Frame-Options: DENY`
- [ ] `Referrer-Policy: strict-origin-when-cross-origin`
- [ ] `Content-Security-Policy` para Razor Pages: `default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:` (refinar conforme front)
- [ ] `Permissions-Policy: geolocation=(), microphone=(), camera=()`
- [ ] `app.UseHttpsRedirection()` em produção
- [ ] Testar headers com securityheaders.com em staging

**Dependências:** SETUP-04
**Bloqueia:** Deploy

**Riscos:**
- CSP muito restritivo quebrar páginas Razor. Mitigação: começar com `Report-Only` durante EP-06 e endurecer no fim.

---

## [SEC-03] CORS configurado por ambiente

- **Lista:** To Do
- **Labels:** `security`, `backend`
- **Prioridade:** Alta
- **Estimativa:** P (1h)
- **Branch sugerida:** `feat/cors`
- **Commit sugerido:** `feat(security): configure CORS policy`

**Descrição:**
Configurar CORS com whitelist de origens via config.

**Critérios de Aceite:**
- [ ] Lê `Cors:AllowedOrigins` (lista) do config
- [ ] Dev: permite `http://localhost:5173`, `http://localhost:5000`
- [ ] Prod: apenas domínios reais do frontend
- [ ] Métodos permitidos: GET, POST, PUT, DELETE, OPTIONS
- [ ] Headers permitidos: `Authorization`, `Content-Type`, `X-Correlation-Id`
- [ ] `AllowCredentials=true` apenas se necessário (Razor Pages mesmo origin não precisa)
- [ ] **Nunca** usar `AllowAnyOrigin()` em prod

**Dependências:** SETUP-03
**Bloqueia:** Deploy

**Riscos:**
- Esquecer de adicionar origem do front em prod. Mitigação: documentar no README de deploy.

---

## [SEC-04] Password Policy — política de senha forte

- **Lista:** To Do
- **Labels:** `security`, `backend`
- **Prioridade:** Crítica
- **Estimativa:** M (3-4h)
- **Branch sugerida:** `feat/password-policy`
- **Commit sugerido:** `feat(security): add strong password policy validator`

**Descrição:**
Implementar `IPasswordValidator` com regras de complexidade reais (não apenas comprimento), validação via FluentValidation e integração com HIBP (delegada ao SEC-05).

**Critérios de Aceite:**
- [ ] Mínimo **12 caracteres**
- [ ] Pelo menos **1 letra maiúscula** (A-Z)
- [ ] Pelo menos **1 letra minúscula** (a-z)
- [ ] Pelo menos **1 dígito** (0-9)
- [ ] Pelo menos **1 caractere especial** da lista: ``!@#$%^&*()-_=+[]{};:'",.<>/?\|`~``
- [ ] **Não pode ser igual** ao username ou email (case-insensitive)
- [ ] **Não pode estar na lista do HIBP** (delegado a SEC-05 — chamada de `IPwnedPasswordsClient`)
- [ ] Mensagens de erro **em PT-BR**, uma por regra violada:
  - "A senha deve ter no mínimo 12 caracteres."
  - "A senha deve conter pelo menos uma letra maiúscula."
  - "A senha deve conter pelo menos uma letra minúscula."
  - "A senha deve conter pelo menos um dígito."
  - "A senha deve conter pelo menos um caractere especial."
  - "A senha não pode ser igual ao seu email/username."
  - "Esta senha aparece em vazamentos públicos conhecidos. Escolha outra."
- [ ] Validador implementado como `PasswordValidator : AbstractValidator<PasswordValidationContext>` onde `PasswordValidationContext { Password, Email, Username }`
- [ ] Reutilizável em: registro (AUTH-01), troca de senha (USER-05), reset de senha (AUTH-09)
- [ ] **Testes unitários cobrindo cada regra individualmente** (caso feliz + caso de borda):
  - Senha com 11 chars rejeitada, 12 aceita
  - Senha sem maiúscula rejeitada
  - Senha sem minúscula rejeitada
  - Senha sem dígito rejeitada
  - Senha sem especial rejeitada
  - Senha igual ao email rejeitada (case-insensitive)
  - Senha igual ao username rejeitada
  - Senha forte mas em HIBP rejeitada (mock do `IPwnedPasswordsClient`)
  - Senha forte e não vazada aceita

**Notas Técnicas:**
- A regra "diferente do email" usa `string.Equals(password, email, StringComparison.OrdinalIgnoreCase)`.
- HIBP é uma chamada externa — fazer com `HttpClient` injetado, com timeout de 2s. Se a API estiver fora, falhar **aberto** (aceitar a senha) e logar warning — não bloquear registro por indisponibilidade externa.

**Dependências:** SETUP-02
**Bloqueia:** AUTH-01, USER-05, AUTH-09

**Riscos:**
- HIBP indisponível → decisão de falhar aberto. Documentar trade-off.

---

## [SEC-05] Integração com HaveIBeenPwned Pwned Passwords API

- **Lista:** To Do
- **Labels:** `security`, `backend`
- **Prioridade:** Alta
- **Estimativa:** M (2-3h)
- **Branch sugerida:** `feat/hibp-pwned-passwords`
- **Commit sugerido:** `feat(security): integrate HaveIBeenPwned Pwned Passwords API`

**Descrição:**
Cliente HTTP para consultar a API pública do **HaveIBeenPwned Pwned Passwords** (https://api.pwnedpasswords.com) usando o modelo **k-anonymity** — envia apenas os 5 primeiros caracteres do SHA-1 do password, recebe lista de sufixos com contagem. A API é gratuita, open, sem chave para uso básico.

**Critérios de Aceite:**
- [ ] Interface `IPwnedPasswordsClient` em Domain: `Task<bool> IsPwnedAsync(string password, CancellationToken ct)`
- [ ] Implementação `PwnedPasswordsClient` em Infrastructure:
  - Calcula `SHA1(password)` em uppercase hex
  - Envia GET para `https://api.pwnedpasswords.com/range/{first5chars}`
  - Header `Add-Padding: true` (recomendado pela HIBP para evitar análise de tamanho de resposta)
  - User-Agent customizado configurado em `Hibp:UserAgent` (ex: `Lumen-Portfolio/1.0`)
  - Timeout de 2s via `HttpClient.Timeout`
  - Parse da resposta (linhas `SUFIXO:COUNT`), procura o sufixo correspondente
  - Retorna `true` se encontrar (vazada) com count > 0
- [ ] Registrado via `services.AddHttpClient<IPwnedPasswordsClient, PwnedPasswordsClient>()`
- [ ] **Fail-open:** se a API der erro/timeout, retorna `false` (aceita a senha) e loga warning estruturado
- [ ] Cache local em memória (`IMemoryCache`) por 1h para o mesmo prefixo, reduzindo chamadas em ambiente de teste
- [ ] **Nunca enviar a senha completa, nem o hash completo** — apenas os 5 primeiros caracteres do hash
- [ ] **Nunca logar** a senha, hash completo, nem o prefixo (poderia ajudar correlação em logs vazados)
- [ ] Testes:
  - Unitário com `HttpMessageHandler` mockado para resposta com sufixo presente → `true`
  - Unitário com sufixo ausente → `false`
  - Unitário com timeout/erro → `false` + log de warning verificado
  - Integração (skip por default, marcado `[Trait("Category","ExternalApi")]`) que chama de verdade com `"password"` esperando `true`

**Notas Técnicas:**
- Documentação oficial: https://haveibeenpwned.com/API/v3#PwnedPasswords
- Sem auth necessária para `/range/{prefix}` no modelo k-anonymity.
- Limite prático: ~1.5M requests/dia sem chave. Suficiente para portfólio.
- SHA-1 aqui não é falha de segurança — é o protocolo da API (não estamos armazenando SHA-1 da senha, só usando para a consulta efêmera).

**Dependências:** SETUP-02, SETUP-03
**Bloqueia:** SEC-04

**Riscos:**
- API fora do ar bloquearia registro se fail-closed. Mitigação: fail-open + log + alerta (out of scope MVP).
- User-Agent vazio retorna 403. Mitigação: validar on-startup que `Hibp:UserAgent` não é vazio.

---

## [SEC-06] Tratamento global de exceções e error responses padronizados

- **Lista:** To Do
- **Labels:** `security`, `backend`, `tech-debt`
- **Prioridade:** Alta
- **Estimativa:** M (2h)
- **Branch sugerida:** `feat/global-exception-handler`
- **Commit sugerido:** `feat(api): add global exception handler with ProblemDetails`

**Descrição:**
Middleware global de exceções que retorna `ProblemDetails` (RFC 7807) sem vazar stack trace em produção.

**Critérios de Aceite:**
- [ ] `app.UseExceptionHandler()` com handler customizado
- [ ] Em prod: `{ type, title: "Internal Server Error", status: 500, traceId }` — sem detalhes internos
- [ ] Em dev: inclui `detail` com mensagem e `extensions.stackTrace`
- [ ] Mapeamento de exceções específicas:
  - `ValidationException` → 400 + lista de erros por campo
  - `UnauthorizedAccessException` → 401
  - `ForbiddenException` (custom) → 403
  - `NotFoundException` (custom) → 404
  - `ConflictException` (custom) → 409
- [ ] `traceId` correlacionado com `X-Correlation-Id` do log
- [ ] Stack trace **nunca** retornado em prod

**Dependências:** SETUP-04
**Bloqueia:** —

**Riscos:** —

---

# EP-06 — Frontend Razor Pages

---

## [FE-01] Setup Razor Pages e layout base

- **Lista:** Backlog
- **Labels:** `frontend`
- **Prioridade:** Média
- **Estimativa:** M (2-3h)
- **Branch sugerida:** `feat/razor-layout`
- **Commit sugerido:** `feat(web): add Razor Pages layout and base styles`

**Descrição:**
Habilitar Razor Pages no `Lumen.Api`, criar `_Layout.cshtml`, navbar, footer, CSS mínimo (sem framework pesado — Bootstrap 5 OU CSS puro com variáveis; recomendar **Pico.css** que é open source, sem JS, leve).

**Critérios de Aceite:**
- [ ] `builder.Services.AddRazorPages()` e `app.MapRazorPages()` no `Program.cs`
- [ ] Pasta `Pages/` com `_ViewStart.cshtml`, `_ViewImports.cshtml`, `Shared/_Layout.cshtml`
- [ ] Layout com header (logo "Lumen"), nav (Login, Registro, Dashboard se logado), footer
- [ ] CSS via `wwwroot/css/site.css` + Pico.css via CDN ou arquivo local
- [ ] Página `Index.cshtml` (landing simples)
- [ ] Responsivo (mobile-first)

**Dependências:** SETUP-01
**Bloqueia:** FE-02..04

**Riscos:** —

---

## [FE-02] Páginas de Login e Registro

- **Lista:** Backlog
- **Labels:** `frontend`
- **Prioridade:** Média
- **Estimativa:** M (3-4h)
- **Branch sugerida:** `feat/razor-auth-pages`
- **Commit sugerido:** `feat(web): add login and register Razor pages`

**Descrição:**
Páginas `/login` e `/register` que consomem os endpoints `/api/auth/login` e `/api/auth/register`. Cookie httpOnly armazena o access token (ou usar autenticação cookie do ASP.NET Core para Razor Pages).

**Critérios de Aceite:**
- [ ] `/login` com form: emailOrUsername, password, botão "Entrar", link "Esqueci minha senha"
- [ ] `/register` com form: email, username, password, confirmPassword
- [ ] Validação client-side básica (HTML5 `required`, `minlength`)
- [ ] Validação server-side via `[BindProperty]` + `ModelState` (FluentValidation integrado)
- [ ] Após login OK: redireciona para `/dashboard`
- [ ] Após registro OK: tela "Confirme seu email" + reenvio possível
- [ ] Mensagens de erro exibidas inline sem revelar detalhes (ex: "Credenciais inválidas")
- [ ] **Antiforgery token** ativo nos forms (Razor Pages já faz por padrão)
- [ ] Access token armazenado em **cookie httpOnly Secure SameSite=Strict**, NÃO em localStorage

**Notas Técnicas:**
- Recomendação: usar cookie auth do ASP.NET Core para Razor Pages e JWT Bearer para API — dois schemes coexistindo via `AddPolicyScheme`.
- Ou simplificar: Razor Pages chama os mesmos services internamente (sem passar pela API HTTP) — mais simples, mas acopla view e domínio. Decidir no SETUP-01.

**Dependências:** FE-01, AUTH-01, AUTH-02
**Bloqueia:** —

**Riscos:**
- XSS expondo cookie. Mitigação: HttpOnly + CSP rígido (SEC-02).

---

## [FE-03] Página de Dashboard (autenticada)

- **Lista:** Backlog
- **Labels:** `frontend`
- **Prioridade:** Média
- **Estimativa:** M (2h)
- **Branch sugerida:** `feat/razor-dashboard`
- **Commit sugerido:** `feat(web): add user dashboard page`

**Descrição:**
Página `/dashboard` mostrando dados do usuário, botão de logout, link para alterar senha, link para alterar perfil.

**Critérios de Aceite:**
- [ ] `[Authorize]` no PageModel
- [ ] Mostra email, username, role, último login, conta criada em
- [ ] Botão "Sair" chama `/api/auth/logout` e limpa cookie
- [ ] Link para `/account/change-password`
- [ ] Link para `/account/profile` (edição básica)

**Dependências:** FE-02, AUTH-06
**Bloqueia:** —

**Riscos:** —

---

## [FE-04] Páginas de recuperação e reset de senha

- **Lista:** Backlog
- **Labels:** `frontend`
- **Prioridade:** Média
- **Estimativa:** M (2-3h)
- **Branch sugerida:** `feat/razor-password-reset`
- **Commit sugerido:** `feat(web): add forgot/reset password pages`

**Descrição:**
Páginas `/forgot-password` e `/reset-password?token=...` que conversam com EP-07.

**Critérios de Aceite:**
- [ ] `/forgot-password`: form com email, mensagem genérica de sucesso ("Se o email existir, enviaremos instruções")
- [ ] `/reset-password?token=...`: form com nova senha + confirmação, valida token via API, exibe erros de política
- [ ] Em sucesso: redireciona para `/login` com mensagem
- [ ] Mensagens sempre genéricas no `/forgot-password` (não confirma existência de email)

**Dependências:** FE-01, AUTH-08, AUTH-09
**Bloqueia:** —

**Riscos:** —

---

# EP-07 — Email, Recuperação, Reset & Ativação

---

## [EMAIL-01] Serviço de envio de email via MailKit

- **Lista:** To Do
- **Labels:** `email`, `backend`
- **Prioridade:** Alta
- **Estimativa:** M (3h)
- **Branch sugerida:** `feat/email-service`
- **Commit sugerido:** `feat(email): add email service using MailKit`

**Descrição:**
Serviço `IEmailService` que envia emails via SMTP usando **MailKit** (MIT, recomendado pela Microsoft). Templates HTML simples com placeholders.

**Critérios de Aceite:**
- [ ] Interface em Domain: `IEmailService.SendAsync(EmailMessage, CancellationToken)`
- [ ] Implementação `MailKitEmailService` em Infrastructure:
  - Lê `SmtpOptions` (`Host`, `Port`, `User`, `Pass`, `From`, `UseStartTls`)
  - Usa `MailKit.Net.Smtp.SmtpClient` + `MimeKit.MimeMessage`
  - Suporta `Multipart/alternative` (text/plain + text/html)
  - Conexão com `SecureSocketOptions.StartTlsWhenAvailable` (Mailpit em dev → sem TLS; prod → TLS)
- [ ] `EmailMessage { To, Subject, HtmlBody, TextBody }`
- [ ] Templates Razor `.cshtml` para emails em `Templates/Email/`: `EmailConfirmation.cshtml`, `PasswordReset.cshtml`, `PasswordChanged.cshtml`
- [ ] Renderização via `RazorViewToStringRenderer` (helper conhecido)
- [ ] Erros de envio são logados (warning) mas **não** propagam exceção para o endpoint que chamou (fire-and-forget com retry simples — 2 tentativas)
- [ ] Smoke test manual: registrar usuário em dev → ver email no Mailpit

**Notas Técnicas:**
- **Por que não `System.Net.Mail.SmtpClient`:** marcado obsoleto pela Microsoft (https://learn.microsoft.com/dotnet/api/system.net.mail.smtpclient), não suporta TLS moderno bem.
- Em ambientes serverless, considerar `BackgroundService` para fila de emails. Para MVP, fire-and-forget com `Task.Run` é aceitável.

**Dependências:** SETUP-02, SETUP-03, SETUP-05 (Mailpit)
**Bloqueia:** AUTH-01, AUTH-08, AUTH-09, USER-05

**Riscos:**
- SMTP timeout bloqueia request. Mitigação: timeout de 10s no `SmtpClient.ConnectAsync`.

---

## [EMAIL-02] Configurar SMTP de produção via env vars

- **Lista:** To Do
- **Labels:** `email`, `infra`, `security`
- **Prioridade:** Alta
- **Estimativa:** P (1-2h)
- **Branch sugerida:** `chore/prod-smtp-env`
- **Commit sugerido:** `chore: document and validate production SMTP configuration`

**Descrição:**
Deixar o app **agnóstico ao provedor SMTP** em produção. Toda configuração lida de env vars padronizadas — o operador escolhe o provedor (Postal self-host, Brevo free, Postmark, qualquer SMTP) sem mudar código.

**Critérios de Aceite:**
- [ ] Env vars documentadas no README e validadas on-startup em `Production`:
  - `SMTP_HOST` (obrigatória, não pode ser `localhost`)
  - `SMTP_PORT` (obrigatória, int, default 587)
  - `SMTP_USER` (obrigatória)
  - `SMTP_PASS` (obrigatória, marcada como secret no deploy)
  - `SMTP_FROM` (obrigatória, email válido)
  - `SMTP_USE_STARTTLS` (default `true`)
- [ ] Mapeamento via `ASPNETCORE` convention: `Smtp__Host`, `Smtp__Port`, etc.
- [ ] Validação on-startup falha o boot se algo essencial faltar em prod
- [ ] README documenta 3 opções de provedor:
  1. **Postal self-hosted em Docker** (100% open source) — link para docs, aviso sobre necessidade de DNS (SPF/DKIM/DMARC) e IP com boa reputação
  2. **SMTP genérico de qualquer provedor com tier gratuito** (Brevo 300/dia, etc.) — explicar que não é open source mas é gratuito, decisão do operador
  3. **MailHog/Mailpit em staging** apontando para domínio interno (para testar pipeline sem mandar email real)
- [ ] Aviso explícito no README: "Para portfólio MVP, qualquer SMTP confiável serve. Postal self-host é a opção 100% open source."

**Notas Técnicas:**
- Não criar abstração extra de "provedor X / provedor Y" — SMTP é o protocolo, MailKit já abstrai. O app só precisa de credenciais SMTP.

**Dependências:** EMAIL-01
**Bloqueia:** Deploy

**Riscos:**
- Provedor SMTP gratuito sumir / mudar política. Mitigação: agnosticismo via env vars permite trocar em minutos.

---

## [AUTH-08] Endpoint de recuperação de senha `POST /api/auth/forgot-password`

- **Lista:** Backlog
- **Labels:** `backend`, `security`, `email`
- **Prioridade:** Média
- **Estimativa:** M (2-3h)
- **Branch sugerida:** `feat/auth-forgot-password`
- **Commit sugerido:** `feat(auth): add forgot password endpoint`

**Descrição:**
Recebe email, gera `PasswordResetToken` válido por 30min, envia email. Resposta sempre genérica para evitar user enumeration.

**Critérios de Aceite:**
- [ ] `POST /api/auth/forgot-password` aceita `{ email }`
- [ ] Resposta SEMPRE 200 OK com `{ message: "Se o email existir, enviaremos instruções" }` (não revelar se email existe)
- [ ] Internamente: se email existe, gera token, salva hash, envia email com link `${FRONTEND_URL}/reset-password?token=${rawToken}`
- [ ] Token expira em 30min, single-use
- [ ] Rate limit aplicado (SEC-01)
- [ ] Logs registram email solicitado (para auditoria), nunca o token

**Dependências:** EMAIL-01, DATA-03, SEC-01
**Bloqueia:** FE-04

**Riscos:**
- Email enumeration. Mitigação: resposta uniforme + rate limit + tempo de resposta constante (executar geração de token dummy mesmo se user não existe).

---

## [AUTH-09] Endpoint de reset `POST /api/auth/reset-password`

- **Lista:** Backlog
- **Labels:** `backend`, `security`
- **Prioridade:** Média
- **Estimativa:** M (2-3h)
- **Branch sugerida:** `feat/auth-reset-password`
- **Commit sugerido:** `feat(auth): add reset password endpoint`

**Descrição:**
Recebe token + nova senha, valida token, aplica política de senha forte, atualiza hash, revoga refresh tokens, envia email de notificação.

**Critérios de Aceite:**
- [ ] `POST /api/auth/reset-password` aceita `{ token, newPassword }`
- [ ] Valida token: existe, `ExpiresAt > now`, `UsedAt == null`
- [ ] Aplica `IPasswordValidator` (SEC-04) na nova senha
- [ ] Atualiza `PasswordHash` com BCrypt cost 12
- [ ] Marca token `UsedAt = now`
- [ ] Revoga todos os refresh tokens do usuário
- [ ] Envia email "Sua senha foi alterada com sucesso"
- [ ] 400 se token inválido/expirado, 400 com lista se senha falhar política
- [ ] Resposta 204 em sucesso

**Dependências:** AUTH-08, SEC-04
**Bloqueia:** FE-04

**Riscos:** —

---

## [AUTH-10] Endpoint de confirmação de email `GET /api/auth/confirm-email`

- **Lista:** Backlog
- **Labels:** `backend`, `email`
- **Prioridade:** Média
- **Estimativa:** P (1-2h)
- **Branch sugerida:** `feat/auth-confirm-email`
- **Commit sugerido:** `feat(auth): add email confirmation endpoint`

**Descrição:**
Endpoint chamado pelo link no email de confirmação. Marca user como ativo.

**Critérios de Aceite:**
- [ ] `GET /api/auth/confirm-email?token=...`
- [ ] Valida `EmailConfirmationToken` (existe, não expirou, não usado)
- [ ] Marca `user.IsActive = true`, `user.EmailConfirmedAt = now`
- [ ] Marca token `UsedAt = now`
- [ ] Redireciona para `/login?confirmed=true` em sucesso (Razor)
- [ ] Mostra página de erro em falha (token inválido, expirado, já usado)
- [ ] Endpoint `POST /api/auth/resend-confirmation` para reenviar (rate limited)

**Dependências:** EMAIL-01, DATA-03
**Bloqueia:** —

**Riscos:**
- Token em GET fica no histórico do browser/logs de servidor. Mitigação: token single-use + curta validade (24h).

---

# EP-08 — Testes

---

## [TEST-01] Setup xUnit + FluentAssertions + estrutura de testes

- **Lista:** To Do
- **Labels:** `tests`, `infra`
- **Prioridade:** Alta
- **Estimativa:** P (1-2h)
- **Branch sugerida:** `chore/test-setup`
- **Commit sugerido:** `chore: setup xUnit and FluentAssertions in test projects`

**Descrição:**
Garantir que projetos de teste compilam e rodam, com convenção de nomenclatura e helpers básicos.

**Critérios de Aceite:**
- [ ] `tests/Lumen.UnitTests/*.csproj` referencia `src/Lumen.Domain` e `src/Lumen.Infrastructure`
- [ ] `tests/Lumen.IntegrationTests/*.csproj` referencia `src/Lumen.Api` (com `Microsoft.AspNetCore.Mvc.Testing`)
- [ ] Convenção: `MethodName_Scenario_ExpectedResult` ou `Should_ExpectedResult_When_Scenario`
- [ ] `dotnet test` executa pelo menos um teste-canário em cada projeto
- [ ] `coverlet.collector` configurado para gerar cobertura

**Dependências:** SETUP-02
**Bloqueia:** TEST-02..04

**Riscos:** —

---

## [TEST-02] Testes unitários — Auth e Password Policy

- **Lista:** Backlog
- **Labels:** `tests`, `security`
- **Prioridade:** Alta
- **Estimativa:** G (5-6h)
- **Branch sugerida:** `test/auth-unit`
- **Commit sugerido:** `test(auth): add unit tests for auth services and password policy`

**Descrição:**
Cobrir com testes unitários: `JwtTokenService`, `PasswordValidator` (regra a regra), `PwnedPasswordsClient`, lockout de login, rotação de refresh token.

**Critérios de Aceite:**
- [ ] `JwtTokenService`: gera token válido, valida com sucesso, rejeita expirado, rejeita assinatura inválida
- [ ] `PasswordValidator`: todas as regras de SEC-04 cobertas (mínimo 1 teste por regra)
- [ ] `PwnedPasswordsClient`: senha vazada detectada, senha não-vazada passa, timeout retorna false (fail-open)
- [ ] Lockout: após 5 falhas, conta bloqueia por 15min
- [ ] Refresh: rotação emite novo, antigo é revogado, reuso de antigo revoga família
- [ ] Cobertura mínima de 80% nas pastas `Lumen.Domain` e serviços críticos de `Lumen.Infrastructure`

**Dependências:** TEST-01, SEC-04, SEC-05, AUTH-02, AUTH-04
**Bloqueia:** Deploy

**Riscos:** —

---

## [TEST-03] Testes de integração com Testcontainers MongoDB

- **Lista:** Backlog
- **Labels:** `tests`
- **Prioridade:** Alta
- **Estimativa:** G (5-7h)
- **Branch sugerida:** `test/integration-mongo`
- **Commit sugerido:** `test: add integration tests using Testcontainers MongoDB`

**Descrição:**
Testes de integração que sobem um MongoDB real via Testcontainers e exercitam endpoints reais via `WebApplicationFactory`.

**Critérios de Aceite:**
- [ ] `IntegrationTestFixture : IAsyncLifetime` sobe `MongoDbContainer` antes da classe
- [ ] `WebApplicationFactory<Program>` customizado injeta a connection string do container
- [ ] Cenários cobertos:
  - Registro de usuário cria registro no Mongo e envia email (Mailpit mock ou `IEmailService` substituído)
  - Login com credenciais corretas retorna 200 + tokens
  - Login com credenciais erradas 401
  - Refresh token rotation
  - Reset de senha end-to-end
  - Email duplicado retorna 409
  - Senha fraca retorna 400 com erros de política
  - Endpoint admin recusa user comum (403)
- [ ] Cada teste usa banco limpo (`DropDatabase` no setup)

**Notas Técnicas:**
- `IEmailService` substituído por um fake em testes para evitar dependência do Mailpit nos CI.

**Dependências:** TEST-01, TEST-02, AUTH-*, USER-*
**Bloqueia:** Deploy

**Riscos:**
- Testcontainers exige Docker no runner CI. Mitigação: documentar no README de CI.

---

## [TEST-04] Smoke tests e teste de carga básico

- **Lista:** Backlog
- **Labels:** `tests`
- **Prioridade:** Média
- **Estimativa:** M (2-3h)
- **Branch sugerida:** `test/smoke-load`
- **Commit sugerido:** `test: add smoke and basic load tests`

**Descrição:**
Smoke test pós-deploy (curl scripts ou Bruno/Postman collection) + teste de carga simples com **k6** (open source, Apache 2.0).

**Critérios de Aceite:**
- [ ] Script `smoke.http` (REST Client / Bruno) cobrindo: register → confirm → login → me → refresh → logout
- [ ] Script `k6/login.js` com 50 VUs por 1min em `/api/auth/login` — métricas no console
- [ ] README documenta como rodar smoke após deploy

**Dependências:** Deploy staging
**Bloqueia:** —

**Riscos:** —

---

# EP-09 — Documentação

---

## [DOC-01] README completo

- **Lista:** Backlog
- **Labels:** `docs`
- **Prioridade:** Média
- **Estimativa:** M (2h)
- **Branch sugerida:** `docs/readme`
- **Commit sugerido:** `docs: write comprehensive README`

**Descrição:**
README final do projeto, suficiente para outro dev clonar e rodar em 10min.

**Critérios de Aceite:**
- [ ] Seções: Sobre, Features, Stack, Requisitos, Como rodar (dev), Variáveis de ambiente, Endpoints principais (resumo), Como rodar testes, Deploy, Licença
- [ ] Comandos prontos: `docker compose up -d`, `dotnet user-secrets set "Jwt:Secret" "..."`, `dotnet run --project src/Lumen.Api`
- [ ] Badges (build status quando tiver CI, license, .NET version)
- [ ] Screenshots das telas Razor

**Dependências:** —
**Bloqueia:** —

**Riscos:** —

---

## [DOC-02] Swagger / OpenAPI completo

- **Lista:** Backlog
- **Labels:** `docs`, `backend`
- **Prioridade:** Média
- **Estimativa:** P (1-2h)
- **Branch sugerida:** `docs/swagger`
- **Commit sugerido:** `docs(api): configure Swagger with security definitions`

**Descrição:**
Habilitar Swagger via Swashbuckle, documentar todos os endpoints, esquema de auth Bearer.

**Critérios de Aceite:**
- [ ] `services.AddSwaggerGen()` + `app.UseSwagger() / UseSwaggerUI()` em Development
- [ ] Esquema `Bearer` configurado (botão "Authorize" no Swagger UI)
- [ ] Todos os endpoints têm `.WithName()`, `.WithSummary()`, `.WithDescription()`, `.Produces<T>()`, `.ProducesProblem()`
- [ ] Tags por feature (Auth, Users, Account)
- [ ] Swagger UI desabilitado em Production (ou protegido por basic auth)

**Dependências:** SETUP-02
**Bloqueia:** —

**Riscos:** —

---

## [DOC-03] ADRs principais

- **Lista:** Backlog
- **Labels:** `docs`
- **Prioridade:** Baixa
- **Estimativa:** P (1-2h)
- **Branch sugerida:** `docs/adrs`
- **Commit sugerido:** `docs: add architecture decision records`

**Descrição:**
ADRs curtos em `docs/adr/` registrando decisões críticas.

**Critérios de Aceite:**
- [ ] ADR-001: Escolha de Minimal APIs vs Controllers
- [ ] ADR-002: BCrypt.Net-Next vs ASP.NET Core PasswordHasher
- [ ] ADR-003: MailKit vs SmtpClient
- [ ] ADR-004: HIBP fail-open
- [ ] ADR-005: Soft delete com janela de 30 dias

**Dependências:** —
**Bloqueia:** —

**Riscos:** —

---

# EP-10 — Deploy

---

## [DEPLOY-01] Dockerfile e .dockerignore

- **Lista:** Backlog
- **Labels:** `infra`
- **Prioridade:** Alta
- **Estimativa:** M (2-3h)
- **Branch sugerida:** `chore/dockerfile`
- **Commit sugerido:** `chore(infra): add multi-stage Dockerfile`

**Descrição:**
Dockerfile multi-stage para produção: build com `mcr.microsoft.com/dotnet/sdk:8.0`, runtime com `mcr.microsoft.com/dotnet/aspnet:8.0-alpine`.

**Critérios de Aceite:**
- [ ] Multi-stage build (`build` → `publish` → `runtime`)
- [ ] Imagem final < 200MB
- [ ] Roda como non-root user (`USER app`)
- [ ] `HEALTHCHECK` apontando para `/health/db`
- [ ] `.dockerignore` exclui `bin/`, `obj/`, `tests/`, `.git/`, `*.md` exceto `README.md`
- [ ] Build local: `docker build -t aegisidentity:local .` passa
- [ ] Run local: `docker run -p 8080:8080 --env-file .env aegisidentity:local` sobe

**Dependências:** SETUP-01..05
**Bloqueia:** DEPLOY-02

**Riscos:** —

---

## [DEPLOY-02] Deploy em Fly.io (free tier)

- **Lista:** Backlog
- **Labels:** `infra`
- **Prioridade:** Alta
- **Estimativa:** M (3-4h)
- **Branch sugerida:** `chore/deploy-flyio`
- **Commit sugerido:** `chore(infra): configure Fly.io deployment`

**Descrição:**
Deploy em **Fly.io** (free tier real, ~3 VMs shared-cpu-1x, 256MB RAM cada, suficiente para portfólio). MongoDB hospedado em **MongoDB Atlas M0** (free).

**Critérios de Aceite:**
- [ ] `fly.toml` na raiz com app name, region, http_service, env vars não-secretas
- [ ] Secrets configurados via `fly secrets set Jwt__Secret=... Mongo__ConnectionString=...`
- [ ] `fly deploy` sobe versão e retorna URL pública
- [ ] HTTPS automático via Fly
- [ ] Smoke test (TEST-04) passa contra URL pública
- [ ] README documenta processo de deploy

**Notas Técnicas:**
- Alternativas se Fly não couber: **Render free tier** (sleeps after 15min idle — ruim pra portfólio) ou **Railway** (trial-based).
- MongoDB Atlas M0: 512MB storage, free forever, suficiente para demo.

**Dependências:** DEPLOY-01, EMAIL-02, todos os EPs anteriores
**Bloqueia:** DEPLOY-03

**Riscos:**
- Fly.io mudar política do free tier. Mitigação: Dockerfile genérico permite migrar para qualquer container host.

---

## [DEPLOY-03] CI/CD via GitHub Actions

- **Lista:** Backlog
- **Labels:** `infra`, `tests`
- **Prioridade:** Média
- **Estimativa:** M (2-3h)
- **Branch sugerida:** `chore/ci-github-actions`
- **Commit sugerido:** `chore(ci): add GitHub Actions workflow for build, test and deploy`

**Descrição:**
Pipeline: build → test (unit + integration com Testcontainers) → publish image → deploy Fly.io.

**Critérios de Aceite:**
- [ ] `.github/workflows/ci.yml`:
  - Trigger: push em `main`, PR em `main`
  - Steps: setup .NET 8, `dotnet restore`, `dotnet build --no-restore`, `dotnet test`
  - Cache de NuGet
  - Docker disponível para Testcontainers
- [ ] `.github/workflows/deploy.yml`:
  - Trigger: push em `main` após CI passar
  - Steps: `flyctl deploy` via `superfly/flyctl-actions/setup-flyctl`
  - Secret `FLY_API_TOKEN` configurado no GitHub
- [ ] Status badge no README

**Dependências:** TEST-01..03, DEPLOY-02
**Bloqueia:** —

**Riscos:** —

---

# Como popular o board no Trello manualmente

1. Abrir https://trello.com/b/2ZZ0yCf8/portifolio-projects
2. Criar listas (se não existirem): `Backlog`, `To Do (Sprint Atual)`, `In Progress`, `Code Review`, `Done`
3. Criar labels com as cores sugeridas (Settings do board → Labels)
4. Para cada card deste arquivo:
   - Copiar título no formato `[CÓDIGO] Título`
   - Colar a descrição completa (do "Descrição" até "Riscos")
   - Aplicar labels listadas
   - Definir checklist com os "Critérios de Aceite"
   - Adicionar na lista indicada em `Lista:`
5. Mover cards de EP-01 + SEC-04 + AUTH-01 para `To Do (Sprint Atual)` como primeira sprint

---

# Sprint Plan recomendado (3 sprints de 1 semana)

**Sprint 1 — Fundação (semana 1):**
SETUP-01, SETUP-02, SETUP-03, SETUP-04, SETUP-05, DATA-01, DATA-02, DATA-03

**Sprint 2 — Auth core e segurança (semana 2):**
AUTH-01, AUTH-02, AUTH-03, AUTH-04, AUTH-05, AUTH-06, SEC-01, SEC-02, SEC-04, SEC-05, EMAIL-01, EMAIL-02

**Sprint 3 — Frontend, recuperação, testes e deploy (semana 3):**
FE-01, FE-02, FE-03, FE-04, AUTH-07..10, USER-01..05, SEC-03, SEC-06, TEST-01..04, DOC-01..03, DEPLOY-01..03

**Caminho crítico do MVP:**
`SETUP-01 → SETUP-02 → SETUP-03 → DATA-01 → DATA-02 → DATA-03 → SEC-05 → SEC-04 → EMAIL-01 → AUTH-03 → AUTH-01 → AUTH-02 → SEC-01 → TEST-01..03 → DEPLOY-01 → DEPLOY-02`

---

# Resumo

- **45 cards** organizados em **10 épicos**
- Stack 100% **.NET 8 + open source** (MailKit, BCrypt.Net-Next, FluentValidation, Serilog, xUnit, Testcontainers)
- **Email:** Mailpit em dev (Docker), SMTP agnóstico em prod via env vars (Postal self-host recomendado para 100% open source)
- **Política de senha forte** com HIBP integrado (k-anonymity, gratuito, sem chave)
- Caminho crítico identificado para entregar MVP em ~3 semanas

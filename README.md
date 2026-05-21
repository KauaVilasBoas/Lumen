# AegisIdentity

Plataforma de gestao de identidade e acesso (IAM) construida em .NET 8 — projeto de portfolio.

> Status: **Bootstrap concluido** — arquitetura em pe, configuracao de ambiente definida, sem dominio implementado.

## O que faz

Sistema de autenticacao e gerenciamento de usuarios com:

- Registro e login com hash BCrypt
- Emissao e renovacao de JWT (access token + refresh token)
- Recuperacao de senha por e-mail (MailKit)
- Backoffice administrativo em Razor Pages

## Stack

| Camada | Tecnologia |
|---|---|
| Runtime | .NET 8 / ASP.NET Core 8 |
| Presentation | Razor Pages + Minimal APIs |
| Auth | Microsoft.AspNetCore.Authentication.JwtBearer |
| Crypto | BCrypt.Net-Next |
| Validacao | FluentValidation |
| E-mail | MailKit |
| Banco de dados | MongoDB |
| Logging | Serilog |
| Testes | xUnit + Testcontainers (planejado) |
| Deploy | Docker + Fly.io (planejado) |

## Arquitetura

Clean Architecture + DDD + CQRS. Regra de dependencia: `Api` e `Infrastructure` dependem de `Domain`. `Domain` nao depende de nada.

```
AegisIdentity/
├── src/
│   ├── AegisIdentity.Api/               Entry point — Razor Pages + Minimal API endpoints
│   │   ├── Endpoints/Dev/               Endpoints dev-only (nunca registrados em prod)
│   │   ├── Middleware/                  CorrelationIdMiddleware e outros middlewares HTTP
│   │   └── Logging/                     Convencoes de logging e dados sensiveis
│   ├── AegisIdentity.Application/       Use cases, interfaces de servico, handlers CQRS
│   ├── AegisIdentity.Domain/            Entidades, Value Objects, contratos de repositorio
│   └── AegisIdentity.Infrastructure/    Persistencia, e-mail, providers externos
│       └── Configuration/               Options classes (JwtOptions, MongoOptions, SmtpOptions, HibpOptions)
├── tests/
│   ├── AegisIdentity.UnitTests/         Domain + Application
│   └── AegisIdentity.IntegrationTests/  Api + Infrastructure
├── docker-compose.yml                   Stack de desenvolvimento (Mailpit + MongoDB)
├── Directory.Build.props                Configuracoes MSBuild centralizadas
├── Directory.Packages.props             Central Package Management
├── .gitignore
├── .editorconfig
├── AegisIdentity.sln
├── CHANGELOG.md
├── LICENSE
└── TASKS_TRELLO.md
```

## Configuracao local

### Desenvolvimento local com Docker

O `docker-compose.yml` na raiz do projeto sobe dois servicos em um unico comando:

| Servico | Proposito | Endereco |
|---|---|---|
| Mailpit | SMTP local + Web UI para inspecionar emails | SMTP: `localhost:1025` / UI: http://localhost:8025 |
| MongoDB | Banco de dados local | `mongodb://localhost:27017` |

```powershell
# Subir ambos os servicos em background
docker compose up -d

# Parar sem remover dados do Mongo
docker compose down

# Parar E limpar o volume do Mongo (reset completo)
docker compose down -v
```

> **Mailpit nao persiste mensagens.** Cada `docker compose down` limpa a caixa de entrada.
> Isso e uma decisao deliberada para evitar confusao entre diferentes sessoes de desenvolvimento.
> Se preferir persistencia, edite o `docker-compose.yml` e descomente o volume `.mailpit-data`.

### Smoke test de email

Com os containers rodando, acesse o endpoint de teste:

```powershell
# HTTP — substitua o destinatario
curl "http://localhost:5237/dev/email-test?to=voce@test.com"

# Resposta esperada
# { "ok": true, "to": "voce@test.com", "viewer": "http://localhost:8025" }
```

Em seguida, abra http://localhost:8025 no browser para verificar o recebimento.

> O endpoint `/dev/email-test` existe **somente** quando `ASPNETCORE_ENVIRONMENT=Development`.
> Ele nunca e registrado em Staging ou Production.

### Pre-requisitos

- .NET 8 SDK
- Docker Desktop (para `docker compose up`)

### Subindo a aplicacao

```powershell
dotnet restore
dotnet run --project src/AegisIdentity.Api
```

A aplicacao sobe em `http://localhost:5237` (HTTP) ou `https://localhost:7068` (HTTPS).

### Variaveis obrigatorias

| Variavel (env var format) | Secao + Chave | Descricao | Exemplo |
|---|---|---|---|
| `Mongo__ConnectionString` | `Mongo:ConnectionString` | URI de conexao ao MongoDB | `mongodb://localhost:27017` |
| `Mongo__Database` | `Mongo:Database` | Nome do banco de dados | `aegisidentity` |
| `Jwt__Issuer` | `Jwt:Issuer` | Emissor do token JWT | `AegisIdentity` |
| `Jwt__Audience` | `Jwt:Audience` | Audiencia do token JWT | `AegisIdentity.Clients` |
| `Jwt__Secret` | `Jwt:Secret` | Chave de assinatura HMAC-SHA256 (min 32 chars) | `<chave-aleatoria-forte>` |
| `Jwt__ExpirationMinutes` | `Jwt:ExpirationMinutes` | Validade do access token em minutos | `15` |
| `Jwt__RefreshExpirationDays` | `Jwt:RefreshExpirationDays` | Validade do refresh token em dias | `7` |
| `Smtp__Host` | `Smtp:Host` | Servidor SMTP | `smtp.sendgrid.net` |
| `Smtp__Port` | `Smtp:Port` | Porta SMTP | `587` |
| `Smtp__User` | `Smtp:User` | Usuario SMTP | `apikey` |
| `Smtp__Pass` | `Smtp:Pass` | Senha / API key SMTP | `<secret>` |
| `Smtp__From` | `Smtp:From` | Endereco remetente | `no-reply@seudominio.com` |
| `Smtp__UseStartTls` | `Smtp:UseStartTls` | Ativar STARTTLS | `true` |
| `Hibp__UserAgent` | `Hibp:UserAgent` | User-Agent para a API HIBP | `SeuApp/1.0 (contact@seudominio.com)` |
| `Hibp__ApiBaseUrl` | `Hibp:ApiBaseUrl` | URL base da API HIBP | `https://api.pwnedpasswords.com` |
| `Cors__AllowedOrigins__0` | `Cors:AllowedOrigins[0]` | Origem permitida pelo CORS | `https://seudominio.com` |

> **Nota:** Todas as variaveis `[Required]` causam falha imediata no startup se ausentes (ValidateOnStart).

### Configuracao local via User Secrets

Use `dotnet user-secrets` para armazenar segredos em desenvolvimento sem commita-los:

```powershell
cd src/AegisIdentity.Api

dotnet user-secrets set "Mongo:ConnectionString" "mongodb://localhost:27017"
dotnet user-secrets set "Mongo:Database" "aegisidentity_dev"
dotnet user-secrets set "Jwt:Secret" "<sua-chave-aleatoria-minimo-32-caracteres>"
dotnet user-secrets set "Smtp:Host" "localhost"
dotnet user-secrets set "Smtp:Port" "1025"
dotnet user-secrets set "Smtp:From" "no-reply@aegisidentity.local"
```

Os segredos ficam em `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` e nunca entram no repositorio.

Para listar os segredos configurados:

```powershell
dotnet user-secrets list --project src/AegisIdentity.Api
```

### Configuracao em producao (env vars)

Em producao, injete os segredos via variaveis de ambiente do provedor (Fly.io, Railway, etc.).
O ASP.NET Core mapeia `Section__Key` para `Section:Key` automaticamente:

```bash
# Fly.io
fly secrets set Mongo__ConnectionString="mongodb+srv://user:pass@cluster/dbname"
fly secrets set Jwt__Secret="sua-chave-forte-de-producao-min-32-chars"
fly secrets set Smtp__Host="smtp.sendgrid.net"
fly secrets set Smtp__Pass="SG.xxxxxxxxxxxxxxxxxxxxx"

# Docker / docker-compose
environment:
  - Mongo__ConnectionString=mongodb://mongo:27017
  - Jwt__Secret=sua-chave-forte
  - Smtp__Host=mailserver
```

> **Nunca** defina segredos reais em `appsettings.json` ou `appsettings.Development.json`.
> Consulte `src/AegisIdentity.Api/appsettings.example.json` para o formato completo de configuracao.

## Logging

### Formato por ambiente

| Ambiente | Sink | Formato |
|---|---|---|
| Producao | Console + File rotativo (daily) | `CompactJsonFormatter` (JSON estruturado) |
| Desenvolvimento | Console apenas | Template legivel: `[HH:mm:ss LVL] Message {Properties}` |

Arquivos de log ficam em `logs/aegis-YYYYMMDD.log`, retidos por 7 dias.

### Correlation ID

Cada request recebe um `X-Correlation-Id`. Se o header vier no request, o valor e preservado.
Se ausente, o middleware gera um Guid no formato `N` (32 hex chars).
O ID aparece em todos os logs do request e no response header `X-Correlation-Id`.

Para rastrear um request especifico:

```powershell
curl -H "X-Correlation-Id: meu-id-de-rastreio" https://localhost:7068/
```

### Dados sensiveis — politica de nao-vazamento

Os seguintes campos NUNCA devem aparecer como argumento estruturado de log:

- `Password`, `PasswordHash`
- `Token`, `AccessToken`, `RefreshToken`
- `ResetCode`, `Secret`

Logue apenas campos seguros (ex: `Email`, `UserId`). Consulte
`src/AegisIdentity.Api/Logging/SensitiveDataConvention.cs` para a lista completa e exemplos.
A enforcement e por convencao e code review. Um filtro automatico sera adicionado na
security hardening card quando os casos de uso correspondentes estiverem implementados.

## Politica de senha

Toda senha aceita pelo sistema (registro, troca, reset) e validada pelo
`IPasswordValidator` em `src/AegisIdentity.Application/Security/`. As regras:

- Minimo **12 caracteres**.
- Pelo menos **uma letra maiuscula**, **uma minuscula**, **um digito** e
  **um caractere especial** da lista ``!@#$%^&*()-_=+[]{};:'",.<>/?\|`~``.
- Nao pode ser igual ao email ou username do usuario (comparacao
  case-insensitive).
- Nao pode aparecer na base **HaveIBeenPwned Pwned Passwords**.

A checagem HIBP usa o modelo **k-anonymity**: o cliente em
`src/AegisIdentity.Infrastructure/Security/PwnedPasswordsClient.cs` envia
somente os 5 primeiros caracteres hex do `SHA1(senha)` para
`https://api.pwnedpasswords.com/range/{prefix}` (com header `Add-Padding: true`).
Resultados sao cacheados em memoria por **1 hora** por prefixo.

O cliente HIBP e **fail-open**: timeouts ou erros da API publica nao bloqueiam
o registro — geram um `Warning` estruturado e a senha e aceita. Isso e uma
decisao deliberada: indisponibilidade externa nunca deve negar acesso ao
proprio sistema. O risco residual e documentado no card SEC-05.

Mensagens de erro sao em PT-BR e cada regra violada gera uma linha
independente na resposta — o usuario ve tudo o que precisa corrigir de
uma vez so.

## Como rodar os testes

```powershell
# Tudo, exceto testes que dependem de servicos externos
dotnet test

# Inclui o teste de integracao que chama a API publica do HaveIBeenPwned
dotnet test --filter "Category=ExternalApi"
```

## Roadmap / Status atual

| Card | Descricao | Status |
|---|---|---|
| SETUP-01 | Estruturar solucao em camadas | Concluido |
| SETUP-02 | Gerenciamento central de pacotes NuGet | Concluido |
| SETUP-03 | Configurar variaveis de ambiente e appsettings | Concluido |
| SETUP-04 | Configurar Serilog para logs estruturados | Concluido |
| SETUP-05 | Setup Mailpit local via Docker Compose | Concluido |
| DATA-01 | Configurar contexto MongoDB e health check | Concluido |
| DATA-02 | Modelar agregado User e persistencia | Concluido |
| DATA-03 | Modelar agregados de tokens (refresh, reset, confirmacao) | Concluido |
| SEC-04 | Politica de senha forte | Concluido |
| SEC-05 | Integracao HaveIBeenPwned Pwned Passwords | Concluido |
| AUTH-01 | Implementar registro e login | Pendente |

Ver [TASKS_TRELLO.md](./TASKS_TRELLO.md) para o backlog completo.

## Limitacoes conhecidas

- Nenhum handler de dominio implementado ainda.
- Sem CI/CD configurado.
- Sem deploy publicado.
- Sem HTTPS em dev (usa HTTP local por padrao via launchSettings).
- O endpoint `/dev/email-test` depende do container Mailpit estar rodando (`docker compose up -d`). Sem ele, o request retorna HTTP 500 com detalhe do erro de conexao SMTP.

## Licenca

[MIT](LICENSE)

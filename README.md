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
│   ├── AegisIdentity.Application/       Use cases, interfaces de servico, handlers CQRS
│   ├── AegisIdentity.Domain/            Entidades, Value Objects, contratos de repositorio
│   └── AegisIdentity.Infrastructure/    Persistencia, e-mail, providers externos
│       └── Configuration/               Options classes (JwtOptions, MongoOptions, SmtpOptions, HibpOptions)
├── tests/
│   ├── AegisIdentity.UnitTests/         Domain + Application
│   └── AegisIdentity.IntegrationTests/  Api + Infrastructure
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

### Pre-requisitos

- .NET 8 SDK
- MongoDB rodando em `localhost:27017` (ou Docker: `docker run -p 27017:27017 mongo`)
- Mailpit para SMTP local (opcional): `docker run -p 1025:1025 -p 8025:8025 axllent/mailpit`

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

## Como rodar os testes

```powershell
dotnet test
```

## Roadmap / Status atual

| Card | Descricao | Status |
|---|---|---|
| SETUP-01 | Estruturar solucao em camadas | Concluido |
| SETUP-02 | Gerenciamento central de pacotes NuGet | Concluido |
| SETUP-03 | Configurar variaveis de ambiente e appsettings | Concluido |
| DATA-01 | Definir banco de dados | Pendente |
| AUTH-01 | Implementar registro e login | Pendente |

Ver [TASKS_TRELLO.md](./TASKS_TRELLO.md) para o backlog completo.

## Limitacoes conhecidas

- Nenhum handler de dominio implementado ainda.
- Sem CI/CD configurado.
- Sem deploy publicado.
- Sem HTTPS em dev (usa HTTP local por padrao via launchSettings).

## Licenca

[MIT](LICENSE)

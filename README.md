# AegisIdentity

Plataforma de gestao de identidade e acesso (IAM) construida em .NET 8 — projeto de portfolio.

> Status: **Bootstrap concluido** — arquitetura em pe, sem dominio implementado.

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

## Como rodar localmente

**Pre-requisitos:** .NET 8 SDK

```powershell
dotnet restore
dotnet run --project src/AegisIdentity.Api
```

A aplicacao sobe em `https://localhost:7068` (HTTPS) ou `http://localhost:5237` (HTTP).

## User Secrets

Os segredos NAO ficam em `appsettings.json`. Configure localmente:

```powershell
cd src/AegisIdentity.Api
dotnet user-secrets set "ConnectionStrings:Default" "<sua-connection-string>"
dotnet user-secrets set "Jwt:Secret" "<chave-aleatoria-min-32-chars>"
dotnet user-secrets set "Smtp:Host" "localhost"
dotnet user-secrets set "Smtp:Port" "1025"
```

## Como rodar os testes

```powershell
dotnet test
```

## Roadmap / Status atual

| Card | Descricao | Status |
|---|---|---|
| SETUP-01 | Estruturar solucao em camadas | Em andamento |
| DATA-01 | Definir banco de dados | Pendente |
| AUTH-01 | Implementar registro e login | Pendente |

Ver [TASKS_TRELLO.md](./TASKS_TRELLO.md) para o backlog completo.

## Limitacoes conhecidas

- Banco de dados nao definido (pendente card DATA-01). Connection string vazia no momento.
- Nenhum handler de dominio implementado ainda.
- Sem CI/CD configurado.
- Sem deploy publicado.

## Licenca

[MIT](LICENSE)

# AegisIdentity

Sistema de gestao de identidade e autenticacao — projeto de portfolio.

> Status: **Bootstrap** — arquitetura em pe, sem dominio implementado.

## O que faz

Plataforma de identidade e acesso (IAM) com autenticacao JWT, gerenciamento de usuarios,
controle de sessoes e envio de e-mails transacionais.

Funcionalidades planejadas:
- Registro e login de usuarios com BCrypt
- Emissao e renovacao de JWT (access + refresh token)
- Recuperacao de senha por e-mail (MailKit)
- Backoffice administrativo em Razor Pages

## Stack

- .NET 8 / ASP.NET Core 8 / Razor Pages
- Clean Architecture + DDD + CQRS
- JWT (Microsoft.AspNetCore.Authentication.JwtBearer)
- BCrypt.Net-Next / FluentValidation / MailKit / Serilog
- xUnit + Testcontainers (planejado)
- Docker + Fly.io (planejado)

## Estrutura

```
AegisIdentity/
├── src/
│   ├── AegisIdentity.Backoffice/        Presentation — Razor Pages
│   ├── AegisIdentity.Application/       Use cases, interfaces de servico
│   ├── AegisIdentity.Domain/            Entidades, Value Objects, contratos
│   └── AegisIdentity.Infrastructure/    Persistencia, e-mail, providers externos
├── tests/
│   ├── AegisIdentity.UnitTests/         Domain + Application
│   └── AegisIdentity.IntegrationTests/  Backoffice + Infrastructure
├── .gitignore
├── .editorconfig
├── AegisIdentity.sln
├── README.md
└── TASKS_TRELLO.md
```

## Como rodar localmente

```powershell
dotnet restore
dotnet run --project src/AegisIdentity.Backoffice
```

## User Secrets

Os segredos NAO ficam em `appsettings.json`. Configure localmente:

```powershell
cd src/AegisIdentity.Backoffice
dotnet user-secrets set "ConnectionStrings:Default" "<sua-connection-string>"
dotnet user-secrets set "Jwt:Secret" "<chave-aleatoria-min-32-chars>"
dotnet user-secrets set "Smtp:Host" "localhost"
dotnet user-secrets set "Smtp:Port" "1025"
```

## Status atual

Bootstrap concluido. Sem dominio implementado.
Ver [TASKS_TRELLO.md](./TASKS_TRELLO.md) para o backlog completo.

## Limitacoes conhecidas

- Banco de dados nao definido (pendente card DATA-01).
- Nenhum handler de dominio implementado.
- Sem CI/CD configurado ainda.

## Licenca

MIT

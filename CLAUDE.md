# AegisIdentity

Plataforma de identidade e autorização (.NET 8) construída com Clean Architecture, DDD e CQRS. Expõe uma **API REST** (`AegisIdentity.Api`) para autenticação/identidade e um **Backoffice MVC** (`AegisIdentity.Backoffice`) para administração. Este documento é a fonte de verdade da stack e das convenções arquiteturais — todo agente e contribuição deve se basear aqui.

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

## Estrutura da solução (`AegisIdentity.sln`)

```
src/
  AegisIdentity.Domain/                 Entidades, agregados, interfaces de repositório, domain services
  SharedKernel/AegisIdentity.SharedKernel/  Constants, Exceptions, Util (cross-cutting, sem dependências de infra)
  Application/
    AegisIdentity.CommandHandlers/      Commands + CommandHandlers (escrita) + Behaviors (ValidationBehavior)
    AegisIdentity.ReadModels/           Queries + QueryHandlers (leitura)
    AegisIdentity.EventHandlers/        Handlers de domain/integration events
  Infrastructure/
    AegisIdentity.DataAccess/           DbContext EF Core, Configurations, Repositories
    AegisIdentity.Integration/          Integrações externas (HTTP, etc.)
  AegisIdentity.Infrastructure/         Composição de infra (Security, DI, serviços)
  Migrations/
    AegisIdentity.Migrations/           Migrations EF Core
    AegisIdentity.Migrations.Cli/       CLI para aplicar migrations
  Jobs/AegisIdentity.Jobs/              Hangfire (registro de jobs e dashboard)
  Presentation/
    AegisIdentity.Backoffice/           Backoffice MVC (Controllers, Views, ViewComponents)
  AegisIdentity.Api/                    Host da API REST (Controllers, Program.cs, DI root)
tests/
  AegisIdentity.UnitTests/              Testes de handlers/validators (xUnit + NSubstitute + FluentAssertions)
  AegisIdentity.IntegrationTests/       Testes de endpoint (WebApplicationFactory + Testcontainers — exigem Docker)
```

## Regras arquiteturais (OBRIGATÓRIAS)

### CQRS — Command (escrita)
- **Command e CommandHandler vivem no MESMO arquivo `.cs`.** O `record` do Command pode estar fora da classe do Handler, mas no mesmo arquivo. Padrão atual: `Command` aninhado como `public sealed record Command(...) : IRequest<Unit>` dentro da classe do Handler.
- **O Controller dispara o Command via MediatR** (`_mediator.Send(command, ct)`). Controller não contém regra de negócio.
- **Dentro de um CommandHandler só é permitido usar Repositories** (escrita via EF Core). Um CommandHandler **nunca** chama um QueryHandler nem acessa a camada de leitura (Dapper).
- Validação via `AbstractValidator<Command>` no mesmo arquivo, aplicada pelo `ValidationBehavior` do MediatR.

### CQRS — Query (leitura)
- **Query e QueryHandler vivem no MESMO arquivo `.cs`** (mesmo padrão do Command).
- **O QueryHandler usa EF Core para leitura**, preferindo `AsNoTracking`. (Decisão de 2026-06-14: Dapper foi descartado; toda a persistência usa EF Core.) Pode acessar o `DbContext`/repositórios de leitura; corrija N+1 com projeção/joins/`Include`.
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

- **Unit** (`AegisIdentity.UnitTests`): handlers e validators com xUnit + NSubstitute + FluentAssertions. Toda nova feature deve ter testes de handler e de validator.
- **Integration** (`AegisIdentity.IntegrationTests`): endpoints via `WebApplicationFactory` + Testcontainers (SQL Server + Redis). **Exigem Docker rodando**; rodam no CI. Quando não executados localmente, declare isso explicitamente.
- **Architecture** (`AegisIdentity.ArchitectureTests`): testes de dependência de assembly via NetArchTest.Rules. Devem ser rodados junto com os unit tests. **Não exigem Docker.**

## Constraints de arquitetura (testes automatizados)

Projeto `tests/AegisIdentity.ArchitectureTests` — rodar com `dotnet test tests/AegisIdentity.ArchitectureTests`.
Cada regra abaixo tem um teste correspondente em `ArchitectureTests.cs`.

| # | Regra | Falha quando |
|---|-------|-------------|
| 01 | Domain não depende de Application | Tipo em `AegisIdentity.Domain` importa namespace de CommandHandlers, ReadModels ou EventHandlers |
| 01b | Domain não depende de Infrastructure | Tipo em `AegisIdentity.Domain` importa `AegisIdentity.DataAccess` ou `AegisIdentity.Infrastructure` |
| 01c | Domain não depende de Presentation | Tipo em `AegisIdentity.Domain` importa `AegisIdentity.Api` ou `AegisIdentity.Backoffice` |
| 02 | SharedKernel não depende de nenhuma camada superior | Tipo em `AegisIdentity.SharedKernel` importa Application, Infrastructure ou Presentation |
| 03 | Application não depende de Infrastructure concreta | `CommandHandlers`, `ReadModels` ou `EventHandlers` importam `AegisIdentity.DataAccess` ou `AegisIdentity.Infrastructure` |
| 04 | Application não depende de Presentation | `CommandHandlers`, `ReadModels` ou `EventHandlers` importam `AegisIdentity.Api` ou `AegisIdentity.Backoffice` |
| 05 | CQRS: CommandHandlers não chamam QueryHandlers | Tipo em `CommandHandlers` importa namespace `AegisIdentity.ReadModels` |
| 06 | CQRS: ReadModels não chamam CommandHandlers | Tipo em `ReadModels` importa namespace `AegisIdentity.CommandHandlers` |
| 07 | API Controllers não referenciam entidades de Domain diretamente | Controller em `AegisIdentity.Api` importa `Domain.Users`, `Domain.Authorization`, `Domain.Tokens` ou `Domain.Audit` |
| 08 | Backoffice Controllers não referenciam entidades de Domain diretamente | Controller em `AegisIdentity.Backoffice` importa `Domain.Users`, `Domain.Tokens` ou `Domain.Audit` |
| 09 | API Controllers não referenciam DataAccess (DbContext, Repositories) | Controller em `AegisIdentity.Api` importa `AegisIdentity.DataAccess` |
| 10 | Application assemblies sem dependência transitiva de DataAccess | Qualquer tipo de Application importa `AegisIdentity.DataAccess` |

> Violação detectada = **build de testes falha**. Corrija a dependência, não o teste.

## Comandos

```bash
dotnet build AegisIdentity.sln                 # build (warnings = erro)
dotnet test tests/AegisIdentity.UnitTests       # testes unitários
dotnet test tests/AegisIdentity.IntegrationTests # integração (requer Docker)
dotnet ef migrations add <Nome> -p src/Migrations/AegisIdentity.Migrations   # nova migration
```

> Decisões arquiteturais não cobertas aqui são definidas durante o desenvolvimento. Ao encontrar um caso novo, proponha uma abordagem com o trade-off (ganhos/perdas), confirme, e então atualize este documento.

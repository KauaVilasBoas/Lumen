# Lumen

Plataforma de identidade e autorização (.NET 8) construída com Clean Architecture, DDD e CQRS. Expõe uma **API REST** (`Lumen.Api`) para autenticação/identidade e um **Backoffice MVC** (`Lumen.Backoffice`) para administração. Este documento é a fonte de verdade da stack e das convenções arquiteturais — todo agente e contribuição deve se basear aqui.

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
  Lumen.Domain/                 Entidades, agregados, interfaces de repositório, domain services
  SharedKernel/Lumen.SharedKernel/  Constants, Exceptions, Util (cross-cutting, sem dependências de infra)
  Application/
    Lumen.CommandHandlers/      Commands + CommandHandlers (escrita) + Behaviors (ValidationBehavior)
    Lumen.ReadModels/           Queries + QueryHandlers (leitura)
    Lumen.EventHandlers/        Handlers de domain/integration events
  Infrastructure/
    Lumen.DataAccess/           DbContext EF Core, Configurations, Repositories
    Lumen.Integration/          Integrações externas (HTTP, etc.)
  Lumen.Infrastructure/         Composição de infra (Security, DI, serviços)
  Migrations/
    Lumen.Migrations/           Migrations EF Core
    Lumen.Migrations.Cli/       CLI para aplicar migrations
  Jobs/Lumen.Jobs/              Hangfire (registro de jobs e dashboard)
  Presentation/
    Lumen.Backoffice/           Backoffice MVC (Controllers, Views, ViewComponents)
  Lumen.Api/                    Host da API REST (Controllers, Program.cs, DI root)
tests/
  Lumen.UnitTests/              Testes de handlers/validators (xUnit + NSubstitute + FluentAssertions)
  Lumen.IntegrationTests/       Testes de endpoint (WebApplicationFactory + Testcontainers — exigem Docker)
```

## Regras arquiteturais (OBRIGATÓRIAS)

### CQRS — Command (escrita)
- **Command e CommandHandler vivem no MESMO arquivo `.cs`.** O `record` do Command pode estar fora da classe do Handler, mas no mesmo arquivo. Padrão atual: `Command` aninhado como `public sealed record Command(...) : IRequest<Unit>` dentro da classe do Handler.
- **O Controller dispara o Command via MediatR** (`_mediator.Send(command, ct)`). Controller não contém regra de negócio.
- **Dentro de um CommandHandler só é permitido usar Repositories** (escrita via EF Core). Um CommandHandler **nunca** chama um QueryHandler nem acessa a camada de leitura (Dapper).
- Validação via `AbstractValidator<Command>` no mesmo arquivo, aplicada pelo `ValidationBehavior` do MediatR.

### Domain Events (DDD)
- **Quem levanta o evento é o aggregate root, não o handler.** Agregados herdam de `AggregateRoot` (`Lumen.Domain.Common`) e chamam `RaiseDomainEvent(...)` dentro dos seus métodos de domínio. CommandHandlers **não** injetam `IPublisher` nem publicam eventos diretamente — apenas orquestram os agregados.
- **O dispatch é transacional.** `LumenDbContext.SaveChangesAsync` publica os eventos das entidades rastreadas via `IPublisher` **somente após o commit ter sucesso** (e somente quando não há transação explícita ambiente). Isso garante que o evento nunca dispare se a escrita no banco falhar. Para o evento ser despachado, o agregado precisa estar rastreado pelo `DbContext` no momento do `SaveChanges`.
- **Um domain event só carrega o que o agregado é dono.** Se o evento precisa de dados de outro agregado (ex.: `Username` + `ProfileName` em `UserProfileAssigned`), modele a operação no root que possui esses dados — `User.AssignProfile(profile)` (o `User` é dono do `Username` e recebe o `Profile`). Mapeie `builder.Ignore(x => x.DomainEvents)` na configuração EF de cada agregado.
- **Cascade cross-agregado vira EventHandler.** Quando uma mudança afeta um *conjunto* de outros agregados descoberto via query (ex.: excluir um `Profile` invalida o cache de N usuários), o root levanta um evento próprio carregando os IDs afetados (`ProfileDeleted(AffectedUserIds)`) e um `INotificationHandler` em `Lumen.EventHandlers` faz o fan-out — nunca o `Profile` levantando eventos sobre `User`s estranhos.

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

- **Unit** (`Lumen.UnitTests`): handlers e validators com xUnit + NSubstitute + FluentAssertions. Toda nova feature deve ter testes de handler e de validator.
- **Integration** (`Lumen.IntegrationTests`): endpoints via `WebApplicationFactory` + Testcontainers (SQL Server + Redis). **Exigem Docker rodando**; rodam no CI. Quando não executados localmente, declare isso explicitamente.
- **Architecture** (`Lumen.ArchitectureTests`): testes de dependência de assembly via NetArchTest.Rules. Devem ser rodados junto com os unit tests. **Não exigem Docker.**

## Constraints de arquitetura (testes automatizados)

Projeto `tests/Lumen.ArchitectureTests` — rodar com `dotnet test tests/Lumen.ArchitectureTests`.
Cada regra abaixo tem um teste correspondente em `ArchitectureTests.cs`.

| # | Regra | Falha quando |
|---|-------|-------------|
| 01 | Domain não depende de Application | Tipo em `Lumen.Domain` importa namespace de CommandHandlers, ReadModels ou EventHandlers |
| 01b | Domain não depende de Infrastructure | Tipo em `Lumen.Domain` importa `Lumen.DataAccess` ou `Lumen.Infrastructure` |
| 01c | Domain não depende de Presentation | Tipo em `Lumen.Domain` importa `Lumen.Api` ou `Lumen.Backoffice` |
| 02 | SharedKernel não depende de nenhuma camada superior | Tipo em `Lumen.SharedKernel` importa Application, Infrastructure ou Presentation |
| 03 | Application não depende de Infrastructure concreta | `CommandHandlers`, `ReadModels` ou `EventHandlers` importam `Lumen.DataAccess` ou `Lumen.Infrastructure` |
| 04 | Application não depende de Presentation | `CommandHandlers`, `ReadModels` ou `EventHandlers` importam `Lumen.Api` ou `Lumen.Backoffice` |
| 05 | CQRS: CommandHandlers não chamam QueryHandlers | Tipo em `CommandHandlers` importa namespace `Lumen.ReadModels` |
| 06 | CQRS: ReadModels não chamam CommandHandlers | Tipo em `ReadModels` importa namespace `Lumen.CommandHandlers` |
| 07 | API Controllers não referenciam entidades de Domain diretamente | Controller em `Lumen.Api` importa `Domain.Users`, `Domain.Authorization`, `Domain.Tokens` ou `Domain.Audit` |
| 08 | Backoffice Controllers não referenciam entidades de Domain diretamente | Controller em `Lumen.Backoffice` importa `Domain.Users`, `Domain.Tokens` ou `Domain.Audit` |
| 09 | API Controllers não referenciam DataAccess (DbContext, Repositories) | Controller em `Lumen.Api` importa `Lumen.DataAccess` |
| 10 | Application assemblies sem dependência transitiva de DataAccess | Qualquer tipo de Application importa `Lumen.DataAccess` |

> Violação detectada = **build de testes falha**. Corrija a dependência, não o teste.

## Comandos

```bash
dotnet build Lumen.sln                 # build (warnings = erro)
dotnet test tests/Lumen.UnitTests       # testes unitários
dotnet test tests/Lumen.IntegrationTests # integração (requer Docker)
dotnet ef migrations add <Nome> -p src/Migrations/Lumen.Migrations   # nova migration
```

> Decisões arquiteturais não cobertas aqui são definidas durante o desenvolvimento. Ao encontrar um caso novo, proponha uma abordagem com o trade-off (ganhos/perdas), confirme, e então atualize este documento.

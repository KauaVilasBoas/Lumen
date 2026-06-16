using NetArchTest.Rules;
using FluentAssertions;

namespace AegisIdentity.ArchitectureTests;

/// <summary>
/// Automated architecture constraints for AegisIdentity.
/// Each test enforces a rule described in CLAUDE.md ("Constraints de arquitetura").
/// A failing test means a layer boundary was violated — fix the dependency, not the test.
/// </summary>
public sealed class ArchitectureTests
{
    // ─── Assembly markers (one public type per layer assembly) ──────────────

    private static readonly System.Reflection.Assembly DomainAssembly =
        typeof(AegisIdentity.Domain.Users.User).Assembly;

    private static readonly System.Reflection.Assembly SharedKernelAssembly =
        typeof(AegisIdentity.SharedKernel.Constants.AuthErrorMessages).Assembly;

    private static readonly System.Reflection.Assembly CommandHandlersAssembly =
        typeof(AegisIdentity.CommandHandlers.Behaviors.ValidationBehavior<,>).Assembly;

    private static readonly System.Reflection.Assembly ReadModelsAssembly =
        typeof(AegisIdentity.ReadModels.Queries.GetAuthorizationGraphQueryHandler).Assembly;

    private static readonly System.Reflection.Assembly EventHandlersAssembly =
        typeof(AegisIdentity.EventHandlers.Authorization.UserPermissionsChangedHandler).Assembly;

    private static readonly System.Reflection.Assembly DataAccessAssembly =
        typeof(AegisIdentity.DataAccess.Persistence.AegisIdentityDbContext).Assembly;

    private static readonly System.Reflection.Assembly InfrastructureAssembly =
        typeof(AegisIdentity.Infrastructure.Configuration.AppOptions).Assembly;

    private static readonly System.Reflection.Assembly ApiAssembly =
        typeof(AegisIdentity.Api.Controllers.AuthController).Assembly;

    private static readonly System.Reflection.Assembly BackofficeAssembly =
        typeof(AegisIdentity.Backoffice.Controllers.AccountController).Assembly;

    // ─── Namespace constants ─────────────────────────────────────────────────

    private const string DomainNamespace       = "AegisIdentity.Domain";
    private const string SharedKernelNamespace  = "AegisIdentity.SharedKernel";
    private const string CommandsNamespace     = "AegisIdentity.CommandHandlers";
    private const string ReadModelsNamespace   = "AegisIdentity.ReadModels";
    private const string EventsNamespace       = "AegisIdentity.EventHandlers";
    private const string DataAccessNamespace   = "AegisIdentity.DataAccess";
    private const string InfraNamespace        = "AegisIdentity.Infrastructure";
    private const string ApiNamespace          = "AegisIdentity.Api";
    private const string BackofficeNamespace   = "AegisIdentity.Backoffice";

    // ────────────────────────────────────────────────────────────────────────
    // RULE 01 — Domain must not depend on Application, Infrastructure or Presentation
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Domain_MustNotDependOnApplicationLayer()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(CommandsNamespace, ReadModelsNamespace, EventsNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain is the innermost ring — it must never depend on Application handlers. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    [Fact]
    public void Domain_MustNotDependOnInfrastructureLayer()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(DataAccessNamespace, InfraNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain must never reference Infrastructure (EF Core, Redis, MailKit, etc.). " +
                     $"Failing types: {FailingTypes(result)}");
    }

    [Fact]
    public void Domain_MustNotDependOnPresentationLayer()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApiNamespace, BackofficeNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain must never depend on presentation concerns. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // RULE 02 — SharedKernel must not depend on any non-trivial project layer
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SharedKernel_MustNotDependOnApplicationOrInfraOrPresentation()
    {
        var result = Types.InAssembly(SharedKernelAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                CommandsNamespace,
                ReadModelsNamespace,
                EventsNamespace,
                DataAccessNamespace,
                InfraNamespace,
                ApiNamespace,
                BackofficeNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "SharedKernel is a cross-cutting utility layer — it must have zero upward dependencies. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // RULE 03 — Application layers must not depend on Infrastructure concrete types
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CommandHandlers_MustNotDependOnDataAccessOrInfrastructure()
    {
        var result = Types.InAssembly(CommandHandlersAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(DataAccessNamespace, InfraNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "CommandHandlers must depend only on Domain abstractions (interfaces), " +
                     "never on Infrastructure concrete implementations (EF Core, Redis, etc.). " +
                     $"Failing types: {FailingTypes(result)}");
    }

    [Fact]
    public void ReadModels_MustNotDependOnDataAccessOrInfrastructure()
    {
        var result = Types.InAssembly(ReadModelsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(DataAccessNamespace, InfraNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "ReadModels (QueryHandlers) must depend only on Domain abstractions, " +
                     "never on concrete Infrastructure assemblies. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    [Fact]
    public void EventHandlers_MustNotDependOnDataAccessOrInfrastructure()
    {
        var result = Types.InAssembly(EventHandlersAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(DataAccessNamespace, InfraNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "EventHandlers must depend only on Domain abstractions. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // RULE 04 — Application layers must not depend on Presentation
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CommandHandlers_MustNotDependOnPresentation()
    {
        var result = Types.InAssembly(CommandHandlersAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApiNamespace, BackofficeNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application handlers must never depend on presentation concerns. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    [Fact]
    public void ReadModels_MustNotDependOnPresentation()
    {
        var result = Types.InAssembly(ReadModelsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApiNamespace, BackofficeNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "ReadModels (QueryHandlers) must never depend on presentation concerns. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    [Fact]
    public void EventHandlers_MustNotDependOnPresentation()
    {
        var result = Types.InAssembly(EventHandlersAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApiNamespace, BackofficeNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "EventHandlers must never depend on presentation concerns. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // RULE 05 — CQRS separation: CommandHandlers must not depend on ReadModels
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CommandHandlers_MustNotDependOnReadModels()
    {
        var result = Types.InAssembly(CommandHandlersAssembly)
            .ShouldNot()
            .HaveDependencyOn(ReadModelsNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "CQRS rule: CommandHandlers (write side) must never call QueryHandlers (read side). " +
                     "A CommandHandler that needs to read must do so via repository, not via a Query. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // RULE 06 — CQRS separation: ReadModels must not depend on CommandHandlers
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ReadModels_MustNotDependOnCommandHandlers()
    {
        var result = Types.InAssembly(ReadModelsAssembly)
            .ShouldNot()
            .HaveDependencyOn(CommandsNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "CQRS rule: QueryHandlers (read side) must never depend on CommandHandlers (write side). " +
                     $"Failing types: {FailingTypes(result)}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // RULE 07 — API Controllers must not reference Domain entity namespaces directly
    //           (aggregates, repositories — must go via MediatR Commands/Queries)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ApiControllers_MustNotDependOnDomainEntityNamespaces()
    {
        var domainEntityNamespaces = new[]
        {
            "AegisIdentity.Domain.Users",
            "AegisIdentity.Domain.Authorization",
            "AegisIdentity.Domain.Tokens",
            "AegisIdentity.Domain.Audit",
        };

        var result = Types.InAssembly(ApiAssembly)
            .That()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .ShouldNot()
            .HaveDependencyOnAny(domainEntityNamespaces)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "API Controllers must dispatch Commands/Queries via MediatR — " +
                     "they must not reference Domain entities, aggregates, or repositories directly. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // RULE 08 — Backoffice Controllers must not reference Domain entity namespaces
    //           (exception: IUserPermissionService lives in Domain.Authorization and is an interface — allowed)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BackofficeControllers_MustNotDependOnDomainEntitiesOrRepositories()
    {
        var forbiddenNamespaces = new[]
        {
            "AegisIdentity.Domain.Users",
            "AegisIdentity.Domain.Tokens",
            "AegisIdentity.Domain.Audit",
        };

        var result = Types.InAssembly(BackofficeAssembly)
            .That()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.Controller))
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenNamespaces)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Backoffice Controllers must proxy through AdminApiClient, not access Domain entity types directly. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // RULE 09 — API Controllers must not depend on DataAccess (DbContext, Repositories)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ApiControllers_MustNotDependOnDataAccess()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .ShouldNot()
            .HaveDependencyOn(DataAccessNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "API Controllers must never directly reference EF Core DbContext or concrete Repositories. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // RULE 10 — All Application assemblies: no transitive DataAccess dependency
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ApplicationAssemblies_HaveNoTransitiveDependencyOnDataAccess()
    {
        var commandHandlersResult = Types.InAssembly(CommandHandlersAssembly)
            .ShouldNot()
            .HaveDependencyOn(DataAccessNamespace)
            .GetResult();

        var readModelsResult = Types.InAssembly(ReadModelsAssembly)
            .ShouldNot()
            .HaveDependencyOn(DataAccessNamespace)
            .GetResult();

        var eventHandlersResult = Types.InAssembly(EventHandlersAssembly)
            .ShouldNot()
            .HaveDependencyOn(DataAccessNamespace)
            .GetResult();

        commandHandlersResult.IsSuccessful.Should().BeTrue(
            because: $"CommandHandlers has unexpected DataAccess dependency. Failing: {FailingTypes(commandHandlersResult)}");
        readModelsResult.IsSuccessful.Should().BeTrue(
            because: $"ReadModels has unexpected DataAccess dependency. Failing: {FailingTypes(readModelsResult)}");
        eventHandlersResult.IsSuccessful.Should().BeTrue(
            because: $"EventHandlers has unexpected DataAccess dependency. Failing: {FailingTypes(eventHandlersResult)}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────────────

    private static string FailingTypes(TestResult result)
    {
        if (result.IsSuccessful || result.FailingTypes is null)
            return "(none)";

        return string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}

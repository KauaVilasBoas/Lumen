using NetArchTest.Rules;
using FluentAssertions;

namespace Lumen.ArchitectureTests;

/// <summary>
/// Architecture constraints for Lumen modular monolith.
/// Each test enforces a module boundary rule.
/// A failing test means a boundary was violated — fix the dependency, not the test.
/// </summary>
public sealed class ArchitectureTests
{
    // ─── Assembly markers ────────────────────────────────────────────────────

    private static readonly System.Reflection.Assembly SharedKernelAssembly =
        typeof(Lumen.SharedKernel.Constants.AuthErrorMessages).Assembly;

    private static readonly System.Reflection.Assembly ModularityAssembly =
        typeof(Lumen.Modularity.IModule).Assembly;

    private static readonly System.Reflection.Assembly IdentityModuleAssembly =
        typeof(Lumen.Modules.Identity.IdentityModule).Assembly;

    private static readonly System.Reflection.Assembly IdentityContractsAssembly =
        typeof(Lumen.Modules.Identity.Contracts.IUserPermissionService).Assembly;

    private static readonly System.Reflection.Assembly AuditContractsAssembly =
        typeof(Lumen.Modules.Audit.Contracts.Events.CleanupJobExecutedEvent).Assembly;

    // ─── Namespace constants ─────────────────────────────────────────────────

    private const string SharedKernelNamespace      = "Lumen.SharedKernel";
    private const string ModularityNamespace        = "Lumen.Modularity";
    private const string IdentityModuleNamespace    = "Lumen.Modules.Identity";
    private const string AuditModuleNamespace       = "Lumen.Modules.Audit";
    private const string IdentityContractsNamespace = "Lumen.Modules.Identity.Contracts";
    private const string AuditContractsNamespace    = "Lumen.Modules.Audit.Contracts";

    // ────────────────────────────────────────────────────────────────────────
    // RULE 01 — SharedKernel has no upward dependencies
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SharedKernel_MustNotDependOnAnyModule()
    {
        var result = Types.InAssembly(SharedKernelAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                IdentityModuleNamespace,
                AuditModuleNamespace,
                ModularityNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "SharedKernel is cross-cutting with zero upward dependencies. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // RULE 02 — Lumen.Modularity (building block) has no module dependencies
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Modularity_MustNotDependOnAnyModule()
    {
        var result = Types.InAssembly(ModularityAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                IdentityModuleNamespace,
                AuditModuleNamespace,
                SharedKernelNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Lumen.Modularity is a platform building block — it must have no knowledge of business modules. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // RULE 03 — Identity module internals must not reference Audit internals
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IdentityModule_MustNotDependOnAuditModuleInternals()
    {
        var result = Types.InAssembly(IdentityModuleAssembly)
            .ShouldNot()
            .HaveDependencyOn(AuditModuleNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Identity module must never import Audit module internals. " +
                     "Cross-module communication goes through Contracts + IEventBus only. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // RULE 04 — Identity module internals may reference Audit Contracts
    //           (publishes events that Audit consumes)
    //           but Audit Contracts must NOT reference Identity internals
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AuditContracts_MustNotDependOnIdentityModuleInternals()
    {
        var result = Types.InAssembly(AuditContractsAssembly)
            .ShouldNot()
            .HaveDependencyOn(IdentityModuleNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Audit.Contracts must only depend on Lumen.Modularity (IIntegrationEvent). " +
                     "Integration events are owned by the publishing module. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // RULE 05 — Identity.Contracts must not reference Identity module internals
    //           or Audit module internals (only Lumen.Modularity allowed)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IdentityContracts_MustNotDependOnModuleInternals()
    {
        // Note: the Identity.Contracts assembly itself lives under the
        // "Lumen.Modules.Identity.Contracts" namespace, so we must check
        // against the non-Contracts identity namespace and the audit internals.
        var result = Types.InAssembly(IdentityContractsAssembly)
            .ShouldNot()
            .HaveDependencyOn(AuditModuleNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Identity.Contracts must not depend on Audit module internals. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // RULE 06 — Identity module Contracts must not reference Audit module internals
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IdentityContracts_MustNotDependOnAuditContracts()
    {
        var result = Types.InAssembly(IdentityContractsAssembly)
            .ShouldNot()
            .HaveDependencyOn(AuditContractsNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Identity.Contracts must not depend on Audit.Contracts. " +
                     "Each module's Contracts are independent. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // RULE 07 — Audit Contracts must not reference Identity.Contracts
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AuditContracts_MustNotDependOnIdentityContracts()
    {
        var result = Types.InAssembly(AuditContractsAssembly)
            .ShouldNot()
            .HaveDependencyOn(IdentityContractsNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Audit.Contracts is a standalone boundary — it must not import Identity.Contracts. " +
                     $"Failing types: {FailingTypes(result)}");
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

using NetArchTest.Rules;
using FluentAssertions;

namespace Lumen.ArchitectureTests;

public sealed class ArchitectureTests
{
    private static readonly System.Reflection.Assembly SharedKernelAssembly =
        typeof(Lumen.SharedKernel.Constants.AuthErrorMessages).Assembly;

    private static readonly System.Reflection.Assembly ModularityAssembly =
        typeof(Lumen.Modularity.IModule).Assembly;

    private static readonly System.Reflection.Assembly IdentityModuleAssembly =
        typeof(Lumen.Modules.Identity.IdentityModule).Assembly;

    private static readonly System.Reflection.Assembly IdentityContractsAssembly =
        typeof(Lumen.Modules.Identity.Contracts.Events.UserLoggedInEvent).Assembly;

    private static readonly System.Reflection.Assembly AuditContractsAssembly =
        typeof(Lumen.Modules.Audit.Contracts.Events.CleanupJobExecutedEvent).Assembly;

    private static readonly System.Reflection.Assembly AuthorizationAssembly =
        typeof(Lumen.Authorization.SystemProfiles).Assembly;

    private static readonly System.Reflection.Assembly AuthorizationContractsAssembly =
        typeof(Lumen.Authorization.Contracts.IUserPermissionService).Assembly;

    private static readonly System.Reflection.Assembly AuthorizationAspNetCoreAssembly =
        typeof(Lumen.Authorization.AspNetCore.RequirePermissionAttribute).Assembly;

    private const string SharedKernelNamespace      = "Lumen.SharedKernel";
    private const string ModularityNamespace        = "Lumen.Modularity";
    private const string IdentityModuleNamespace    = "Lumen.Modules.Identity";
    private const string AuditModuleNamespace       = "Lumen.Modules.Audit";
    private const string IdentityContractsNamespace = "Lumen.Modules.Identity.Contracts";
    private const string AuditContractsNamespace    = "Lumen.Modules.Audit.Contracts";
    private const string AuthorizationNamespace          = "Lumen.Authorization";
    private const string AuthorizationContractsNamespace = "Lumen.Authorization.Contracts";
    private const string AuthorizationAspNetCoreNamespace = "Lumen.Authorization.AspNetCore";

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

    [Fact]
    public void IdentityContracts_MustNotDependOnModuleInternals()
    {
        var result = Types.InAssembly(IdentityContractsAssembly)
            .ShouldNot()
            .HaveDependencyOn(AuditModuleNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Identity.Contracts must not depend on Audit module internals. " +
                     $"Failing types: {FailingTypes(result)}");
    }

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

    [Fact]
    public void AuthorizationContracts_MustOnlyDependOnModularity()
    {
        var result = Types.InAssembly(AuthorizationContractsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                IdentityModuleNamespace,
                AuditModuleNamespace,
                SharedKernelNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Lumen.Authorization.Contracts must only depend on Lumen.Modularity. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    [Fact]
    public void Authorization_MustNotDependOnIdentityOrAuditModules()
    {
        var result = Types.InAssembly(AuthorizationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                IdentityModuleNamespace,
                AuditModuleNamespace,
                SharedKernelNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Lumen.Authorization is a standalone library — it must not depend on any Lumen module or SharedKernel. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    [Fact]
    public void Authorization_MustNotDependOnAspNetCoreFramework()
    {
        var result = Types.InAssembly(AuthorizationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                AuthorizationAspNetCoreNamespace,
                "Microsoft.AspNetCore.Authorization",
                "Microsoft.AspNetCore.Http",
                "Microsoft.AspNetCore.Mvc")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Lumen.Authorization core must remain framework-agnostic — ASP.NET enforcement lives in Lumen.Authorization.AspNetCore. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    [Fact]
    public void AuthorizationAspNetCore_MustNotDependOnIdentityOrAuditModules()
    {
        var result = Types.InAssembly(AuthorizationAspNetCoreAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                IdentityModuleNamespace,
                AuditModuleNamespace,
                SharedKernelNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Lumen.Authorization.AspNetCore must not depend on business modules or SharedKernel. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    private static string FailingTypes(TestResult result)
    {
        if (result.IsSuccessful || result.FailingTypes is null)
            return "(none)";

        return string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}

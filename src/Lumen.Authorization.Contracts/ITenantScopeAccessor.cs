namespace Lumen.Authorization.Contracts;

/// <summary>
/// Resolves the active tenant/scope identifier for the current request or execution context.
/// </summary>
/// <remarks>
/// This is an optional extension point for multi-tenant hosts. The default no-op implementation
/// returns <c>null</c>, which preserves the existing global-authorization behavior for
/// applications that do not use tenants.
///
/// Multi-tenant hosts register their own implementation via the DI container:
/// <code>
/// services.AddScoped&lt;ITenantScopeAccessor, MyTenantScopeAccessor&gt;();
/// </code>
/// The scope identifier is an opaque <see cref="Guid"/>; the Lumen library does not interpret
/// its business meaning. It can represent a company, an organization, a workspace, etc.
/// </remarks>
public interface ITenantScopeAccessor
{
    /// <summary>
    /// Returns the active scope identifier, or <c>null</c> if there is no active scope
    /// (global authorization context).
    /// </summary>
    Guid? GetCurrentScopeId();
}
